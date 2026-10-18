using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;
using Warranty.Services.Interfaces.Complete;

namespace Warranty.Services.Implementations.Complete;

public sealed class PaymentService(
    IPaymentRepository paymentRepository,
    IInvoiceRepository invoiceRepository) : IPaymentService
{
    public async Task<PaymentResponseDto> CreateAsync(
        int invoiceId,
        PaymentCreateDto request,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role != UserRole.Receptionist)
        {
            throw new ForbiddenException("Only Receptionists can record payments.");
        }

        if (request.Amount <= 0)
        {
            throw new BusinessRuleException("Payment amount must be greater than zero.");
        }

        if (!Enum.IsDefined(request.Method))
        {
            throw new BusinessRuleException("Payment method is invalid.");
        }

        var note = request.Note?.Trim();
        if (note?.Length > 500)
        {
            throw new BusinessRuleException("Payment note must not exceed 500 characters.");
        }

        var payment = new Payment
        {
            InvoiceId = invoiceId,
            Amount = request.Amount,
            Method = request.Method,
            Note = string.IsNullOrEmpty(note) ? null : note,
            ReceivedById = userId,
            PaidAt = DateTime.UtcNow
        };

        await using var transaction = await paymentRepository.BeginSerializableTransactionAsync(cancellationToken);
        var invoice = await invoiceRepository.GetByIdAsync(
            invoiceId, tracking: true, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Invoice was not found.");
        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
        {
            throw new ConflictException("Payments cannot be recorded for a Paid or Cancelled invoice.");
        }

        var totalPaid = await paymentRepository.GetTotalByInvoiceIdAsync(invoiceId, cancellationToken);
        if (request.Amount > invoice.TotalAmount - totalPaid)
        {
            throw new BusinessRuleException("Payment amount cannot exceed the outstanding balance.");
        }

        await paymentRepository.AddAsync(payment, cancellationToken);
        await paymentRepository.SaveChangesAsync(cancellationToken);
        totalPaid += request.Amount;
        if (totalPaid >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }

        await invoiceRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var paymentDto = (await paymentRepository.GetByInvoiceIdAsync(invoiceId, cancellationToken))
            .Single(item => item.Id == payment.Id);
        return ToResponse(paymentDto);
    }

    public async Task<IReadOnlyList<PaymentResponseDto>> GetByInvoiceIdAsync(
        int invoiceId,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var invoice = await invoiceRepository.GetByIdAsync(invoiceId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Invoice was not found.");
        if (role is not (UserRole.Admin or UserRole.Manager or UserRole.Receptionist) &&
            !(role == UserRole.Customer && invoice.RepairRequest.WarrantyCard.CustomerId == userId))
        {
            throw new ForbiddenException("You are not allowed to view payments for this invoice.");
        }

        return (await paymentRepository.GetByInvoiceIdAsync(invoiceId, cancellationToken))
            .Select(ToResponse)
            .ToList();
    }

    private static PaymentResponseDto ToResponse(Payment payment) => new(
        payment.Id,
        payment.InvoiceId,
        payment.Amount,
        payment.Method,
        payment.Note,
        payment.PaidAt,
        payment.ReceivedById,
        payment.ReceivedBy.FullName);
}
