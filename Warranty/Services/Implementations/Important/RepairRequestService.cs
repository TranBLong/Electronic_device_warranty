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
    IInvoiceRepository invoiceRepository) : IRepairRequestService
{
    public async Task<IReadOnlyList<RepairRequestResponse>> GetAllAsync(
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var requests = role switch
        {
            UserRole.Customer => await repairRequestRepository.GetByCustomerIdAsync(userId, cancellationToken),
            UserRole.Technician => await repairRequestRepository.GetByTechnicianIdAsync(userId, cancellationToken),
            UserRole.Admin or UserRole.Manager or UserRole.Receptionist =>
                await repairRequestRepository.GetAllAsync(cancellationToken),
            _ => throw new ForbiddenException("You are not allowed to view repair requests.")
        };
        return requests.Select(ToResponse).ToList();
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
        request.StatusHistory
            .OrderBy(history => history.ChangedAt)
            .Select(history => new RepairStatusHistoryResponse(
                history.Id, history.OldStatus, history.NewStatus, history.ChangedBy, history.ChangedAt))
            .ToList());
}
