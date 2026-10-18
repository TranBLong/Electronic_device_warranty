using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Repositories.Interfaces.Worth;
using Warranty.Services.Interfaces.Complete;

namespace Warranty.Services.Implementations.Complete;

public sealed class InvoiceService(
    IInvoiceRepository invoiceRepository,
    IRepairRequestRepository repairRequestRepository,
    IRepairRequestPartRepository repairRequestPartRepository) : IInvoiceService
{
    public async Task<InvoiceResponseDto> CreateAsync(
        int repairRequestId,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role != UserRole.Receptionist)
        {
            throw new ForbiddenException("Only Receptionists can issue invoices.");
        }

        var repairRequest = await repairRequestRepository.GetByIdAsync(repairRequestId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Repair request was not found.");
        if (repairRequest.Status is not (RepairRequestStatus.Completed or RepairRequestStatus.Returned))
        {
            throw new BusinessRuleException("An invoice can be issued only for a Completed or Returned repair request.");
        }

        if (await invoiceRepository.ExistsForRepairRequestAsync(repairRequestId, cancellationToken))
        {
            throw new ConflictException("An invoice already exists for this repair request.");
        }

        var parts = await repairRequestPartRepository.GetByRepairRequestIdAsync(repairRequestId, cancellationToken);
        var partsTotal = parts.Sum(item => item.Quantity * item.UnitPrice);
        var laborTotal = repairRequest.LaborCost;
        var totalAmount = repairRequest.IsChargeable ? partsTotal + laborTotal : 0m;
        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            RepairRequestId = repairRequestId,
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            PartsTotal = partsTotal,
            LaborTotal = laborTotal,
            TotalAmount = totalAmount,
            IssuedById = userId,
            IssuedAt = now,
            Status = totalAmount == 0m ? InvoiceStatus.Paid : InvoiceStatus.Unpaid,
            PaidAt = totalAmount == 0m ? now : null
        };

        await invoiceRepository.AddAsync(invoice, cancellationToken);
        try
        {
            await invoiceRepository.SaveChangesWithNotificationAsync(
                new Notification
                {
                    UserId = repairRequest.WarrantyCard.CustomerId,
                    RepairRequestId = repairRequest.Id,
                    Type = NotificationType.InvoiceIssued,
                    Title = $"Invoice issued for repair request #{repairRequest.Id}",
                    Message = $"Invoice {invoice.InvoiceNumber} has been issued."
                },
                cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            if (await invoiceRepository.ExistsForRepairRequestAsync(repairRequestId, cancellationToken))
            {
                throw new ConflictException("An invoice already exists for this repair request.");
            }

            throw;
        }

        return ToResponse(await GetInvoiceAsync(invoice.Id, cancellationToken));
    }

    public async Task<InvoiceResponseDto> GetByIdAsync(
        int id,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var invoice = await GetInvoiceAsync(id, cancellationToken);
        EnsureCanView(invoice, userId, role);
        return ToResponse(invoice);
    }

    public async Task<InvoiceResponseDto> GetByRepairRequestIdAsync(
        int repairRequestId,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByRepairRequestIdAsync(repairRequestId, cancellationToken)
            ?? throw new NotFoundException("Invoice was not found.");
        EnsureCanView(invoice, userId, role);
        return ToResponse(invoice);
    }

    public async Task<PagedResponseDto<InvoiceListResponseDto>> GetAllAsync(
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);
        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw new BusinessRuleException("Invoice status is invalid.");
        }

        var invoices = await invoiceRepository.GetAllAsync(status, page, pageSize, cancellationToken);
        var count = await invoiceRepository.CountAsync(status, null, cancellationToken);
        return new PagedResponseDto<InvoiceListResponseDto>(invoices.Select(ToListResponse).ToList(), page, pageSize, count);
    }

    public async Task<PagedResponseDto<InvoiceListResponseDto>> GetMineAsync(
        int customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);
        var invoices = await invoiceRepository.GetForCustomerAsync(customerId, page, pageSize, cancellationToken);
        var count = await invoiceRepository.CountAsync(null, customerId, cancellationToken);
        return new PagedResponseDto<InvoiceListResponseDto>(invoices.Select(ToListResponse).ToList(), page, pageSize, count);
    }

    public async Task CancelAsync(
        int id,
        int actorUserId,
        UserRole role,
        string? cancellationReason,
        CancellationToken cancellationToken = default)
    {
        if (role is not (UserRole.Receptionist or UserRole.Manager))
        {
            throw new ForbiddenException("Only Receptionists and Managers can cancel invoices.");
        }

        var reason = cancellationReason?.Trim();
        if (reason?.Length > 500)
        {
            throw new BusinessRuleException("Cancellation reason must not exceed 500 characters.");
        }

        var invoice = await invoiceRepository.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException("Invoice was not found.");
        if (invoice.Status != InvoiceStatus.Unpaid || invoice.Payments.Count != 0)
        {
            throw new ConflictException("Only unpaid invoices without payments can be cancelled.");
        }

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.CancelledAt = DateTime.UtcNow;
        invoice.CancelledById = actorUserId;
        invoice.CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
        await invoiceRepository.SaveChangesWithNotificationAsync(
            new Notification
            {
                UserId = invoice.RepairRequest.WarrantyCard.CustomerId,
                RepairRequestId = invoice.RepairRequestId,
                Type = NotificationType.InvoiceCancelled,
                Title = $"Invoice cancelled for repair request #{invoice.RepairRequestId}",
                Message = $"Invoice {invoice.InvoiceNumber} was cancelled."
            },
            cancellationToken);
    }

    private async Task<Invoice> GetInvoiceAsync(int id, CancellationToken cancellationToken) =>
        await invoiceRepository.GetByIdAsync(id, cancellationToken: cancellationToken)
        ?? throw new NotFoundException("Invoice was not found.");

    private static void EnsureCanView(Invoice invoice, int userId, UserRole role)
    {
        if (role is UserRole.Admin or UserRole.Manager or UserRole.Receptionist)
        {
            return;
        }

        if (role == UserRole.Customer && invoice.RepairRequest.WarrantyCard.CustomerId == userId)
        {
            return;
        }

        throw new ForbiddenException("You are not allowed to view this invoice.");
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
        {
            throw new BusinessRuleException("Page must be positive and pageSize must be between 1 and 100.");
        }
    }

    private static InvoiceResponseDto ToResponse(Invoice invoice)
    {
        var paid = invoice.Payments.Sum(payment => payment.Amount);
        return new InvoiceResponseDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.RepairRequestId,
            invoice.Status,
            invoice.PartsTotal,
            invoice.LaborTotal,
            invoice.TotalAmount,
            paid,
            Math.Max(0m, invoice.TotalAmount - paid),
            invoice.IssuedAt,
            invoice.IssuedById,
            invoice.IssuedBy.FullName,
            invoice.PaidAt,
            invoice.CancelledAt,
            invoice.CancelledById,
            invoice.CancellationReason,
            invoice.RepairRequest.Parts
                .OrderBy(item => item.Id)
                .Select(item => new InvoiceLineResponseDto(
                    item.Id, item.PartId, item.Part.Name, item.Part.PartNumber,
                    item.Quantity, item.UnitPrice, item.Quantity * item.UnitPrice))
                .ToList());
    }

    private static InvoiceListResponseDto ToListResponse(Invoice invoice)
    {
        var paid = invoice.Payments.Sum(payment => payment.Amount);
        return new InvoiceListResponseDto(
            invoice.Id, invoice.InvoiceNumber, invoice.RepairRequestId, invoice.Status,
            invoice.TotalAmount, paid, Math.Max(0m, invoice.TotalAmount - paid), invoice.IssuedAt);
    }
}
