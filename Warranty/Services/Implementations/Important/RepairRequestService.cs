using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Repositories.Interfaces.Complete;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Services.Implementations.Important;

public sealed class RepairRequestService(
    IRepairRequestRepository repairRequestRepository,
    IWarrantyCardRepository warrantyCardRepository,
    IUserRepository userRepository,
    IServiceCenterRepository serviceCenterRepository,
    IInvoiceRepository invoiceRepository,
    ILogger<RepairRequestService>? logger = null) : IRepairRequestService
{
    public async Task<PagedResult<RepairRequestResponse>> GetAllAsync(
        int userId,
        UserRole role,
        RepairRequestStatus? status,
        int? technicianId,
        DateTime? createdFrom,
        DateTime? createdTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);
        int? customerId = null;
        int? assignedTechnicianId = null;
        switch (role)
        {
            case UserRole.Customer: customerId = userId; break;
            case UserRole.Technician: assignedTechnicianId = userId; break;
            case UserRole.Admin:
            case UserRole.Manager:
            case UserRole.Receptionist: break;
            default: throw new ForbiddenException("You are not allowed to view repair requests.");
        }
        var requests = await repairRequestRepository.SearchAsync(customerId, assignedTechnicianId, status,
            technicianId, createdFrom, createdTo, page, pageSize, cancellationToken);
        var total = await repairRequestRepository.CountSearchAsync(customerId, assignedTechnicianId, status,
            technicianId, createdFrom, createdTo, cancellationToken);
        return new PagedResult<RepairRequestResponse>(requests.Select(ToResponse).ToList(), page, pageSize, total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize));
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new BusinessRuleException("Page must be positive and pageSize must be between 1 and 100.");
    }

    public async Task<RepairRequestResponse> GetByIdAsync(
        int id,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var request = await GetRequestAsync(id, cancellationToken);
        EnsureCanView(request, userId, role);
        return ToResponse(request);
    }

    public async Task<RepairRequestResponse> CreateAsync(
        CreateRepairRequest request,
        int actorId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role is not (UserRole.Customer or UserRole.Receptionist))
        {
            throw new ForbiddenException("Only Customers and Receptionists can create repair requests.");
        }

        var card = await warrantyCardRepository.GetByIdAsync(request.WarrantyCardId, cancellationToken)
            ?? throw new BusinessRuleException("Warranty card was not found.");
        if (role == UserRole.Customer && card.CustomerId != actorId)
        {
            throw new ForbiddenException("You are not allowed to create a request for this warranty card.");
        }

        if (card.Status != WarrantyStatus.Active || card.EndDate.Date < DateTime.UtcNow.Date)
        {
            throw new BusinessRuleException("Repair requests require an active, unexpired warranty.");
        }

        if (request.ServiceCenterId.HasValue)
        {
            var center = await serviceCenterRepository.GetByIdAsync(
                request.ServiceCenterId.Value, cancellationToken: cancellationToken);
            if (center is null || !center.IsActive)
            {
                throw new BusinessRuleException("An active service center is required.");
            }
        }

        var repairRequest = new RepairRequest
        {
            WarrantyCardId = card.Id,
            ServiceCenterId = request.ServiceCenterId,
            ReceptionistId = role == UserRole.Receptionist ? actorId : null,
            Description = request.Description.Trim(),
            Status = RepairRequestStatus.Received
        };
        repairRequest.StatusHistory.Add(new RepairRequestStatusHistory
        {
            OldStatus = null,
            NewStatus = RepairRequestStatus.Received,
            ChangedBy = actorId
        });

        await repairRequestRepository.AddAsync(repairRequest, cancellationToken);
        await repairRequestRepository.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Repair request {RepairRequestId} created by user {UserId}.", repairRequest.Id, actorId);
        return ToResponse(await GetRequestAsync(repairRequest.Id, cancellationToken));
    }

    public async Task<RepairRequestResponse> AssignServiceCenterAsync(
        int id,
        int serviceCenterId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role is not (UserRole.Receptionist or UserRole.Manager or UserRole.Admin))
        {
            throw new ForbiddenException("Only Receptionists, Managers and Admins can assign a service center.");
        }

        var center = await serviceCenterRepository.GetByIdAsync(serviceCenterId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Service center was not found.");
        if (!center.IsActive)
        {
            throw new BusinessRuleException("The service center is inactive.");
        }

        var request = await GetRequestAsync(id, cancellationToken, tracking: true);
        request.ServiceCenterId = center.Id;
        request.ServiceCenter = center;
        request.UpdatedAt = DateTime.UtcNow;
        await repairRequestRepository.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Service center {ServiceCenterId} assigned to repair request {RepairRequestId}.", center.Id, id);
        return ToResponse(request);
    }

    public async Task<RepairRequestResponse> AssignTechnicianAsync(
        int id,
        int technicianId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role is not (UserRole.Receptionist or UserRole.Manager))
        {
            throw new ForbiddenException("Only Receptionists and Managers can assign Technicians.");
        }

        var request = await GetRequestAsync(id, cancellationToken, tracking: true);
        var technician = await userRepository.GetByIdAsync(technicianId, cancellationToken: cancellationToken);
        if (technician is null || technician.Role != UserRole.Technician || !technician.IsActive)
        {
            throw new BusinessRuleException("An active Technician account is required.");
        }

        request.TechnicianId = technician.Id;
        request.Technician = technician;
        request.UpdatedAt = DateTime.UtcNow;
        await repairRequestRepository.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Technician {TechnicianId} assigned to repair request {RepairRequestId}.", technician.Id, id);
        return ToResponse(request);
    }

    public async Task<RepairRequestResponse> ChangeStatusAsync(
        int id,
        RepairRequestStatus status,
        int actorId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role is not (UserRole.Technician or UserRole.Receptionist))
        {
            throw new ForbiddenException("Only assigned Technicians or Receptionists can update repair request status.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new BusinessRuleException("Repair request status is invalid.");
        }

        var request = await GetRequestAsync(id, cancellationToken, tracking: true);
        if (role == UserRole.Technician && request.TechnicianId != actorId)
        {
            throw new ForbiddenException("Only the assigned Technician can update this repair request.");
        }

        var allowedTransition = (request.Status, status) switch
        {
            (RepairRequestStatus.Received, RepairRequestStatus.InProgress or RepairRequestStatus.Cancelled) => true,
            (RepairRequestStatus.InProgress, RepairRequestStatus.Completed or RepairRequestStatus.Cancelled) => true,
            (RepairRequestStatus.Completed, RepairRequestStatus.Returned) => true,
            _ => false
        };
        if (!allowedTransition)
        {
            throw new BusinessRuleException("The repair request status transition is not allowed.");
        }

        if (role == UserRole.Receptionist &&
            (request.Status != RepairRequestStatus.Completed || status != RepairRequestStatus.Returned))
        {
            throw new ForbiddenException("Receptionists can only return a Completed repair request.");
        }

        var oldStatus = request.Status;
        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        request.StatusHistory.Add(new RepairRequestStatusHistory
        {
            OldStatus = oldStatus,
            NewStatus = status,
            ChangedBy = actorId
        });
        await repairRequestRepository.SaveStatusChangeAsync(
            request,
            new Notification
            {
                UserId = request.WarrantyCard.CustomerId,
                RepairRequestId = request.Id,
                Type = NotificationType.RepairStatusChanged,
                Title = $"Repair request #{request.Id} status updated",
                Message = $"Repair request #{request.Id} status changed to {status}."
            },
            cancellationToken);
        logger?.LogInformation(
            "Repair request {RepairRequestId} status changed from {OldStatus} to {NewStatus} by user {UserId}.",
            id, oldStatus, status, actorId);
        return ToResponse(request);
    }

    public async Task<RepairRequestResponse> SetChargeabilityAsync(
        int id,
        RepairRequestChargeabilityDto request,
        int actorId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role is not (UserRole.Technician or UserRole.Manager))
        {
            throw new ForbiddenException("Only the assigned Technician or a Manager can set repair charges.");
        }

        if (request.LaborCost < 0 || (!request.IsChargeable && request.LaborCost != 0))
        {
            throw new BusinessRuleException("Labor cost must be non-negative and zero when the repair is not chargeable.");
        }

        var repairRequest = await GetRequestAsync(id, cancellationToken, tracking: true);
        if (role == UserRole.Technician && repairRequest.TechnicianId != actorId)
        {
            throw new ForbiddenException("Only the assigned Technician can set charges for this repair request.");
        }

        if (repairRequest.Status is not (RepairRequestStatus.InProgress or RepairRequestStatus.Completed))
        {
            throw new BusinessRuleException("Charges can be set only while the repair request is InProgress or Completed.");
        }

        if (await invoiceRepository.ExistsForRepairRequestAsync(id, cancellationToken))
        {
            throw new ConflictException("Charges cannot be changed after an invoice has been issued.");
        }

        repairRequest.IsChargeable = request.IsChargeable;
        repairRequest.LaborCost = request.LaborCost;
        repairRequest.UpdatedAt = DateTime.UtcNow;
        await repairRequestRepository.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Chargeability updated for repair request {RepairRequestId} by user {UserId}.", id, actorId);
        return ToResponse(repairRequest);
    }

    public async Task<RepairRequestResponse> UpdateDetailsAsync(
        int id,
        UpdateRepairRequestDetailsDto request,
        int actorId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var repairRequest = await GetRequestAsync(id, cancellationToken, tracking: true);

        if (repairRequest.Status is not (RepairRequestStatus.Received or RepairRequestStatus.InProgress or RepairRequestStatus.Completed))
        {
            throw new BusinessRuleException("Details can only be updated when the repair request is Received, InProgress or Completed.");
        }

        if (role == UserRole.Manager)
        {
            // Managers have full access to repair details.
        }
        else if (role == UserRole.Technician && repairRequest.TechnicianId == actorId)
        {
            // Assigned technicians have full access to repair details.
        }
        else if (role == UserRole.Receptionist && repairRequest.Status == RepairRequestStatus.Received)
        {
            if (request.DiagnosisNote is not null || request.Resolution is not null || request.EstimatedCompletionDate is not null)
            {
                throw new ForbiddenException("Receptionists can only set DeviceCondition while the request is Received.");
            }
        }
        else
        {
            throw new ForbiddenException("Only the assigned Technician, a Manager, or a Receptionist (DeviceCondition only) can update details.");
        }

        if (request.EstimatedCompletionDate is { } estimated && estimated.Date < repairRequest.CreatedAt.Date)
        {
            throw new BusinessRuleException("EstimatedCompletionDate cannot be earlier than the creation date.");
        }

        if (request.DeviceCondition is not null)
            repairRequest.DeviceCondition = request.DeviceCondition.Trim();
        if (request.DiagnosisNote is not null)
            repairRequest.DiagnosisNote = request.DiagnosisNote.Trim();
        if (request.Resolution is not null)
            repairRequest.Resolution = request.Resolution.Trim();
        if (request.EstimatedCompletionDate is not null)
            repairRequest.EstimatedCompletionDate = request.EstimatedCompletionDate;

        repairRequest.UpdatedAt = DateTime.UtcNow;
        await repairRequestRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(repairRequest);
    }

    private async Task<RepairRequest> GetRequestAsync(
        int id,
        CancellationToken cancellationToken,
        bool tracking = false) =>
        await repairRequestRepository.GetByIdAsync(id, tracking, cancellationToken)
        ?? throw new NotFoundException("Repair request was not found.");

    private static void EnsureCanView(RepairRequest request, int userId, UserRole role)
    {
        if (role == UserRole.Customer && request.WarrantyCard.CustomerId == userId)
        {
            return;
        }

        if (role == UserRole.Technician && request.TechnicianId == userId)
        {
            return;
        }

        if (role is UserRole.Admin or UserRole.Manager or UserRole.Receptionist)
        {
            return;
        }

        throw new ForbiddenException("You are not allowed to view this repair request.");
    }

    private static RepairRequestResponse ToResponse(RepairRequest request) => new(
        request.Id,
        request.WarrantyCardId,
        request.WarrantyCard.CustomerId,
        request.WarrantyCard.Customer.FullName,
        request.ReceptionistId,
        request.TechnicianId,
        request.Technician?.FullName,
        request.Description,
        request.Status,
        request.CreatedAt,
        request.UpdatedAt,
        request.ServiceCenterId,
        request.IsChargeable,
        request.LaborCost,
        request.DeviceCondition,
        request.DiagnosisNote,
        request.Resolution,
        request.EstimatedCompletionDate,
        request.StatusHistory
            .OrderBy(history => history.ChangedAt)
            .Select(history => new RepairStatusHistoryResponse(
                history.Id, history.OldStatus, history.NewStatus, history.ChangedBy, history.ChangedAt))
            .ToList());
}
