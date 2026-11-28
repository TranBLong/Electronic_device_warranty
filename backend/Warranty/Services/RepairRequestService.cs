using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Services;

public sealed class RepairRequestService(AppDbContext dbContext) : IRepairRequestService
{
    public async Task<ServiceResult<RepairRequestResponse>> CreateAsync(
        CreateRepairRequest request,
        int actorId,
        bool isCustomer,
        bool isReceptionist,
        CancellationToken cancellationToken)
    {
        var warrantyCard = await dbContext.WarrantyCards
            .Include(card => card.Product)
            .Include(card => card.Customer)
            .SingleOrDefaultAsync(card => card.Id == request.WarrantyCardId, cancellationToken);
        if (warrantyCard is null)
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.Invalid, "Warranty card was not found.");
        }

        if (isCustomer && warrantyCard.CustomerId != actorId)
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.Forbidden, "Customer does not own this warranty card.");
        }

        if (warrantyCard.Status != WarrantyStatus.Active || warrantyCard.EndDate.Date < DateTime.UtcNow.Date)
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.Invalid, "Repair requests require an active, unexpired warranty.");
        }

        var repairRequest = new RepairRequest
        {
            WarrantyCardId = warrantyCard.Id,
            ReceptionistId = isReceptionist ? actorId : null,
            Description = request.Description.Trim(),
            Status = RepairRequestStatus.Received
        };
        repairRequest.StatusHistory.Add(new RepairRequestStatusHistory
        {
            OldStatus = null,
            NewStatus = RepairRequestStatus.Received,
            ChangedBy = actorId
        });
        dbContext.RepairRequests.Add(repairRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        repairRequest.WarrantyCard = warrantyCard;
        return ServiceResult<RepairRequestResponse>.Success(RepairRequestMapping.ToResponse(repairRequest));
    }

    public async Task<ServiceResult<RepairRequestResponse>> AssignTechnicianAsync(
        int repairRequestId,
        int technicianId,
        CancellationToken cancellationToken)
    {
        var repairRequest = await GetRequestQuery(tracking: true)
            .SingleOrDefaultAsync(candidate => candidate.Id == repairRequestId, cancellationToken);
        if (repairRequest is null)
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.NotFound, "Repair request was not found.");
        }

        var technician = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Id == technicianId && user.Role == UserRole.Technician && user.IsActive,
            cancellationToken);
        if (technician is null)
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.Invalid, "An active Technician account is required.");
        }

        repairRequest.TechnicianId = technician.Id;
        repairRequest.Technician = technician;
        repairRequest.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<RepairRequestResponse>.Success(RepairRequestMapping.ToResponse(repairRequest));
    }

    public async Task<ServiceResult<RepairRequestResponse>> ChangeStatusAsync(
        int repairRequestId,
        ChangeRepairStatusRequest request,
        int actorId,
        bool isTechnician,
        bool isManagerOrAdmin,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.Invalid, "Repair request status is invalid.");
        }

        var repairRequest = await GetRequestQuery(tracking: true)
            .SingleOrDefaultAsync(candidate => candidate.Id == repairRequestId, cancellationToken);
        if (repairRequest is null)
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.NotFound, "Repair request was not found.");
        }

        if (isTechnician && !isManagerOrAdmin && repairRequest.TechnicianId != actorId)
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.Forbidden, "Technicians can only update assigned requests.");
        }

        if (repairRequest.Status == request.Status)
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.Invalid, "The repair request already has this status.");
        }

        if (request.Status == RepairRequestStatus.Returned &&
            repairRequest.Status != RepairRequestStatus.Completed)
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.Invalid, "A repair request can be returned only after completion.");
        }

        if (repairRequest.Status is RepairRequestStatus.Returned or RepairRequestStatus.Cancelled)
        {
            return ServiceResult<RepairRequestResponse>.Failure(
                ServiceErrorCode.Invalid, "A terminal repair request cannot change status.");
        }

        var oldStatus = repairRequest.Status;
        repairRequest.Status = request.Status;
        repairRequest.UpdatedAt = DateTime.UtcNow;
        repairRequest.StatusHistory.Add(new RepairRequestStatusHistory
        {
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ChangedBy = actorId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<RepairRequestResponse>.Success(RepairRequestMapping.ToResponse(repairRequest));
    }

    public async Task<ServiceResult<RepairPartLineResponse>> AddPartAsync(
        int repairRequestId,
        AddRepairPartRequest request,
        int actorId,
        bool isTechnician,
        bool isManagerOrAdmin,
        CancellationToken cancellationToken)
    {
        var repairRequest = await dbContext.RepairRequests
            .Include(candidate => candidate.WarrantyCard)
            .SingleOrDefaultAsync(candidate => candidate.Id == repairRequestId, cancellationToken);
        if (repairRequest is null)
        {
            return ServiceResult<RepairPartLineResponse>.Failure(
                ServiceErrorCode.NotFound, "Repair request was not found.");
        }

        if (isTechnician && repairRequest.TechnicianId != actorId && !isManagerOrAdmin)
        {
            return ServiceResult<RepairPartLineResponse>.Failure(
                ServiceErrorCode.Forbidden, "Technicians can only add parts to assigned requests.");
        }

        if (repairRequest.Status is RepairRequestStatus.Returned or RepairRequestStatus.Cancelled)
        {
            return ServiceResult<RepairPartLineResponse>.Failure(
                ServiceErrorCode.Invalid, "Cannot add parts to a closed request.");
        }

        var part = await dbContext.Parts.SingleOrDefaultAsync(
            candidate => candidate.Id == request.PartId && candidate.IsActive, cancellationToken);
        if (part is null)
        {
            return ServiceResult<RepairPartLineResponse>.Failure(
                ServiceErrorCode.Invalid, "Part not found or inactive.");
        }

        if (request.Quantity < 1)
        {
            return ServiceResult<RepairPartLineResponse>.Failure(
                ServiceErrorCode.Invalid, "Quantity must be at least 1.");
        }

        if (part.StockQuantity < request.Quantity)
        {
            return ServiceResult<RepairPartLineResponse>.Failure(
                ServiceErrorCode.Invalid, "Insufficient stock.");
        }

        var line = await dbContext.RepairRequestParts.SingleOrDefaultAsync(
            item => item.RepairRequestId == repairRequestId && item.PartId == part.Id,
            cancellationToken);

        part.StockQuantity -= request.Quantity;
        if (line is null)
        {
            line = new RepairRequestPart
            {
                RepairRequestId = repairRequestId,
                PartId = part.Id,
                Quantity = request.Quantity,
                UnitPrice = part.UnitPrice
            };
            dbContext.RepairRequestParts.Add(line);
        }
        else
        {
            line.Quantity += request.Quantity;
        }

        repairRequest.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<RepairPartLineResponse>.Success(new RepairPartLineResponse(
            line.Id,
            part.Id,
            part.Code,
            part.Name,
            line.Quantity,
            line.UnitPrice,
            line.Quantity * line.UnitPrice,
            line.AddedAt));
    }

    private IQueryable<RepairRequest> GetRequestQuery(bool tracking = false)
    {
        var query = dbContext.RepairRequests.AsQueryable();
        if (!tracking) query = query.AsNoTracking();

        return query.Include(request => request.WarrantyCard)
            .ThenInclude(card => card.Customer)
            .Include(request => request.Technician)
            .Include(request => request.StatusHistory.OrderBy(history => history.ChangedAt));
    }
}

internal static class RepairRequestMapping
{
    public static RepairRequestResponse ToResponse(RepairRequest request) => new(
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
        request.StatusHistory
            .OrderBy(history => history.ChangedAt)
            .Select(history => new RepairStatusHistoryResponse(
                history.Id,
                history.OldStatus,
                history.NewStatus,
                history.ChangedBy,
                history.ChangedAt))
            .ToList());
}
