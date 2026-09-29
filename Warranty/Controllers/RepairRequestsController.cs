using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Services;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/repair-requests")]
public sealed class RepairRequestsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RepairRequestResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var requests = GetRequestQuery();
        if (User.IsInRole(nameof(UserRole.Customer)))
        {
            if (!User.TryGetUserId(out var customerId))
            {
                return Unauthorized();
            }

            requests = requests.Where(request => request.WarrantyCard.CustomerId == customerId);
        }
        else if (User.IsInRole(nameof(UserRole.Technician)))
        {
            if (!User.TryGetUserId(out var technicianId))
            {
                return Unauthorized();
            }

            requests = requests.Where(request => request.TechnicianId == technicianId);
        }
        else if (!User.IsInRole(nameof(UserRole.Admin)) &&
                 !User.IsInRole(nameof(UserRole.Manager)) &&
                 !User.IsInRole(nameof(UserRole.Receptionist)))
        {
            return Forbid();
        }

        return Ok(await requests.OrderByDescending(request => request.CreatedAt)
            .Select(request => ToResponse(request))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepairRequestResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var request = await GetRequestQuery()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (request is null)
        {
            return NotFound();
        }

        if (User.IsInRole(nameof(UserRole.Customer)))
        {
            if (!User.TryGetUserId(out var customerId))
            {
                return Unauthorized();
            }

            if (request.WarrantyCard.CustomerId != customerId)
            {
                return Forbid();
            }
        }
        else if (User.IsInRole(nameof(UserRole.Technician)))
        {
            if (!User.TryGetUserId(out var technicianId))
            {
                return Unauthorized();
            }

            if (request.TechnicianId != technicianId)
            {
                return Forbid();
            }
        }
        else if (!User.IsInRole(nameof(UserRole.Admin)) &&
                 !User.IsInRole(nameof(UserRole.Manager)) &&
                 !User.IsInRole(nameof(UserRole.Receptionist)))
        {
            return Forbid();
        }

        return Ok(ToResponse(request));
    }

    [HttpPost]
    public async Task<ActionResult<RepairRequestResponse>> Create(
        CreateRepairRequest request,
        CancellationToken cancellationToken)
    {
        var isCustomer = User.IsInRole(nameof(UserRole.Customer));
        var isReceptionist = User.IsInRole(nameof(UserRole.Receptionist));
        if (!isCustomer && !isReceptionist)
        {
            return Forbid();
        }

        if (!User.TryGetUserId(out var actorId))
        {
            return Unauthorized();
        }

        var warrantyCard = await dbContext.WarrantyCards
            .Include(card => card.Product)
            .Include(card => card.Customer)
            .SingleOrDefaultAsync(card => card.Id == request.WarrantyCardId, cancellationToken);
        if (warrantyCard is null)
        {
            return BadRequest(new ProblemDetails { Title = "Warranty card was not found." });
        }

        if (isCustomer && warrantyCard.CustomerId != actorId)
        {
            return Forbid();
        }

        if (warrantyCard.Status != WarrantyStatus.Active || warrantyCard.EndDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new ProblemDetails { Title = "Repair requests require an active, unexpired warranty." });
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
        return CreatedAtAction(nameof(GetById), new { id = repairRequest.Id }, ToResponse(repairRequest));
    }

    [Authorize(Roles = "Receptionist,Manager")]
    [HttpPut("{id:int}/technician")]
    public async Task<ActionResult<RepairRequestResponse>> AssignTechnician(
        int id,
        AssignTechnicianRequest request,
        CancellationToken cancellationToken)
    {
        var repairRequest = await GetRequestQuery(tracking: true)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (repairRequest is null)
        {
            return NotFound();
        }

        var technician = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Id == request.TechnicianId && user.Role == UserRole.Technician && user.IsActive,
            cancellationToken);
        if (technician is null)
        {
            return BadRequest(new ProblemDetails { Title = "An active Technician account is required." });
        }

        repairRequest.TechnicianId = technician.Id;
        repairRequest.Technician = technician;
        repairRequest.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(repairRequest));
    }

    [Authorize(Roles = nameof(UserRole.Technician))]
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<RepairRequestResponse>> ChangeStatus(
        int id,
        ChangeRepairStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            return BadRequest(new ProblemDetails { Title = "Repair request status is invalid." });
        }

        if (!User.TryGetUserId(out var technicianId))
        {
            return Unauthorized();
        }

        var repairRequest = await GetRequestQuery(tracking: true)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (repairRequest is null)
        {
            return NotFound();
        }

        if (repairRequest.TechnicianId != technicianId)
        {
            return Forbid();
        }

        if (repairRequest.Status == request.Status)
        {
            return BadRequest(new ProblemDetails { Title = "The repair request already has this status." });
        }

        if (request.Status == RepairRequestStatus.Returned &&
            repairRequest.Status != RepairRequestStatus.Completed)
        {
            return BadRequest(new ProblemDetails { Title = "A repair request can be returned only after completion." });
        }

        if (repairRequest.Status is RepairRequestStatus.Returned or RepairRequestStatus.Cancelled)
        {
            return BadRequest(new ProblemDetails { Title = "A terminal repair request cannot change status." });
        }

        var oldStatus = repairRequest.Status;
        repairRequest.Status = request.Status;
        repairRequest.UpdatedAt = DateTime.UtcNow;
        repairRequest.StatusHistory.Add(new RepairRequestStatusHistory
        {
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ChangedBy = technicianId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(repairRequest));
    }

    private IQueryable<RepairRequest> GetRequestQuery(bool tracking = false)
    {
        var query = dbContext.RepairRequests.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.Include(request => request.WarrantyCard)
            .ThenInclude(card => card.Customer)
        .Include(request => request.Technician)
        .Include(request => request.StatusHistory.OrderBy(history => history.ChangedAt));
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
