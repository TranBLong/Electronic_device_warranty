using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Repositories.Interfaces.Complete;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Repositories.Interfaces.Worth;
using Warranty.Services.Interfaces.Worth;

namespace Warranty.Services.Implementations.Worth;

public sealed class RepairRequestPartService(
    IRepairRequestPartRepository repairRequestPartRepository,
    IRepairRequestRepository repairRequestRepository,
    IPartRepository partRepository,
    IInvoiceRepository invoiceRepository) : IRepairRequestPartService
{
    public async Task<IReadOnlyList<RepairRequestPartResponseDto>> GetByRepairRequestIdAsync(
        int repairRequestId,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var repairRequest = await repairRequestRepository.GetByIdAsync(repairRequestId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Repair request was not found.");
        EnsureCanView(repairRequest, userId, role);
        return (await repairRequestPartRepository.GetByRepairRequestIdAsync(repairRequestId, cancellationToken))
            .Select(ToResponse)
            .ToList();
    }

    public async Task<RepairRequestPartResponseDto> AddAsync(
        int repairRequestId,
        RepairRequestPartCreateDto request,
        int technicianId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role != UserRole.Technician)
        {
            throw new ForbiddenException("Only Technicians can add parts to repair requests.");
        }

        if (request.Quantity <= 0)
        {
            throw new BusinessRuleException("Part quantity must be greater than zero.");
        }

        var repairRequest = await repairRequestRepository.GetByIdAsync(
            repairRequestId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Repair request was not found.");
        if (repairRequest.TechnicianId != technicianId)
        {
            throw new ForbiddenException("Only the assigned Technician can add parts to this repair request.");
        }

        if (repairRequest.Status != RepairRequestStatus.InProgress)
        {
            throw new BusinessRuleException("Parts can be added only while the repair request is InProgress.");
        }

        if (await invoiceRepository.ExistsForRepairRequestAsync(repairRequestId, cancellationToken))
        {
            throw new ConflictException("Parts cannot be added after an invoice has been issued.");
        }

        var part = await partRepository.GetByIdAsync(request.PartId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Part was not found.");
        if (request.Quantity > part.StockQuantity)
        {
            throw new BusinessRuleException("The requested quantity exceeds the available stock.");
        }

        var usage = await repairRequestPartRepository.AddWithStockDecrementAsync(
            repairRequestId, part.Id, request.Quantity, cancellationToken);
        if (usage is null)
        {
            throw new BusinessRuleException("The requested quantity exceeds the available stock.");
        }

        return ToResponse(usage);
    }

    private static void EnsureCanView(Models.RepairRequest repairRequest, int userId, UserRole role)
    {
        if (role is UserRole.Admin or UserRole.Manager or UserRole.Receptionist)
        {
            return;
        }

        if (role == UserRole.Technician && repairRequest.TechnicianId == userId)
        {
            return;
        }

        if (role == UserRole.Customer && repairRequest.WarrantyCard.CustomerId == userId)
        {
            return;
        }

        throw new ForbiddenException("You are not allowed to view parts for this repair request.");
    }

    private static RepairRequestPartResponseDto ToResponse(Models.RepairRequestPart item) => new(
        item.Id,
        item.RepairRequestId,
        item.PartId,
        item.Part.Name,
        item.Part.PartNumber,
        item.Quantity,
        item.UnitPrice,
        item.Quantity * item.UnitPrice,
        item.UsedAt);
}
