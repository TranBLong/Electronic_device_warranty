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
    public async Task<ActionResult<PagedResult<RepairRequestResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var requests = GetRequestQuery();

        if (User.IsInRole(nameof(UserRole.Customer)))
        {
            if (!User.TryGetUserId(out var customerId)) return Unauthorized();
            requests = requests.Where(request => request.WarrantyCard.CustomerId == customerId);
        }
        else if (User.IsInRole(nameof(UserRole.Technician)))
        {
            if (!User.TryGetUserId(out var technicianId)) return Unauthorized();
            // Technician sees assigned jobs + unassigned (để nhận việc nếu cần)
            requests = requests.Where(request =>
                request.TechnicianId == technicianId || request.TechnicianId == null);
        }
        else if (!User.IsInAnyRole(
                     nameof(UserRole.Admin),
                     nameof(UserRole.Manager),
                     nameof(UserRole.Receptionist)))
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RepairRequestStatus>(status, true, out var statusFilter))
        {
            requests = requests.Where(r => r.Status == statusFilter);
        }

        if (query.NormalizedKeyword is { } term)
        {
            requests = requests.Where(r =>
                r.Description.ToLower().Contains(term) ||
                r.WarrantyCard.Customer.FullName.ToLower().Contains(term) ||
                (r.Technician != null && r.Technician.FullName.ToLower().Contains(term)));
        }

        requests = (query.SortBy?.ToLowerInvariant()) switch
        {
            "status" => query.IsDescending
                ? requests.OrderByDescending(r => r.Status).ThenByDescending(r => r.Id)
                : requests.OrderBy(r => r.Status).ThenBy(r => r.Id),
            "customer" or "customername" => query.IsDescending
                ? requests.OrderByDescending(r => r.WarrantyCard.Customer.FullName).ThenByDescending(r => r.Id)
                : requests.OrderBy(r => r.WarrantyCard.Customer.FullName).ThenBy(r => r.Id),
            "description" => query.IsDescending
                ? requests.OrderByDescending(r => r.Description).ThenByDescending(r => r.Id)
                : requests.OrderBy(r => r.Description).ThenBy(r => r.Id),
            "technician" or "technicianname" => query.IsDescending
                ? requests.OrderByDescending(r => r.Technician!.FullName).ThenByDescending(r => r.Id)
                : requests.OrderBy(r => r.Technician!.FullName).ThenBy(r => r.Id),
            "updated" or "updatedat" => query.IsDescending
                ? requests.OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.Id)
                : requests.OrderBy(r => r.UpdatedAt).ThenBy(r => r.Id),
            "id" => query.IsDescending ? requests.OrderByDescending(r => r.Id) : requests.OrderBy(r => r.Id),
            _ => query.IsDescending
                ? requests.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
                : requests.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
        };

        var total = await requests.CountAsync(cancellationToken);
        var items = await requests
            .Skip(query.Skip)
            .Take(query.Take)
            .Select(request => ToResponse(request))
            .ToListAsync(cancellationToken);
        return Ok(PagedResult<RepairRequestResponse>.Create(items, query.PageNumber, query.Take, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepairRequestResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var request = await GetRequestQuery()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (request is null) return NotFound();

        if (!CanView(request)) return Forbid();
        return Ok(ToResponse(request));
    }

    [HttpPost]
    public async Task<ActionResult<RepairRequestResponse>> Create(
        CreateRepairRequest request,
        CancellationToken cancellationToken)
    {
        var isCustomer = User.IsInRole(nameof(UserRole.Customer));
        var isStaff = User.IsInAnyRole(
            nameof(UserRole.Admin),
            nameof(UserRole.Manager),
            nameof(UserRole.Receptionist));
        if (!isCustomer && !isStaff) return Forbid();

        if (!User.TryGetUserId(out var actorId)) return Unauthorized();

        var warrantyCard = await dbContext.WarrantyCards
            .Include(card => card.Product)
            .Include(card => card.Customer)
            .SingleOrDefaultAsync(card => card.Id == request.WarrantyCardId, cancellationToken);
        if (warrantyCard is null)
        {
            return BadRequest(new ProblemDetails { Title = "Warranty card was not found." });
        }

        if (isCustomer && warrantyCard.CustomerId != actorId) return Forbid();

        if (warrantyCard.Status != WarrantyStatus.Active || warrantyCard.EndDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new ProblemDetails { Title = "Repair requests require an active, unexpired warranty." });
        }

        var repairRequest = new RepairRequest
        {
            WarrantyCardId = warrantyCard.Id,
            ReceptionistId = User.IsInRole(nameof(UserRole.Receptionist)) ? actorId : null,
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

    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpPut("{id:int}/technician")]
    public async Task<ActionResult<RepairRequestResponse>> AssignTechnician(
        int id,
        AssignTechnicianRequest request,
        CancellationToken cancellationToken)
    {
        var repairRequest = await GetRequestQuery(tracking: true)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (repairRequest is null) return NotFound();

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

    /// <summary>
    /// Technician cập nhật status job được gán; Admin/Manager cũng được phép override.
    /// </summary>
    [Authorize(Roles = "Admin,Manager,Technician")]
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

        if (!User.TryGetUserId(out var actorId)) return Unauthorized();

        var repairRequest = await GetRequestQuery(tracking: true)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (repairRequest is null) return NotFound();

        var isTech = User.IsInRole(nameof(UserRole.Technician));
        var isManagerOrAdmin = User.IsInAnyRole(nameof(UserRole.Admin), nameof(UserRole.Manager));

        if (isTech && !isManagerOrAdmin)
        {
            if (repairRequest.TechnicianId != actorId)
            {
                return Forbid();
            }
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
            ChangedBy = actorId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(repairRequest));
    }

    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RepairRequestResponse>> Update(
        int id,
        UpdateRepairRequestBody request,
        CancellationToken cancellationToken)
    {
        var repairRequest = await GetRequestQuery(tracking: true)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (repairRequest is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            repairRequest.Description = request.Description.Trim();
        }

        repairRequest.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(repairRequest));
    }

    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorId)) return Unauthorized();

        var repairRequest = await GetRequestQuery(tracking: true)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (repairRequest is null) return NotFound();

        if (repairRequest.Status is RepairRequestStatus.Returned)
        {
            return BadRequest(new ProblemDetails { Title = "Cannot cancel a returned request." });
        }

        if (repairRequest.Status != RepairRequestStatus.Cancelled)
        {
            var old = repairRequest.Status;
            repairRequest.Status = RepairRequestStatus.Cancelled;
            repairRequest.UpdatedAt = DateTime.UtcNow;
            repairRequest.StatusHistory.Add(new RepairRequestStatusHistory
            {
                OldStatus = old,
                NewStatus = RepairRequestStatus.Cancelled,
                ChangedBy = actorId
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }


    [HttpGet("{id:int}/parts")]
    public async Task<ActionResult<IReadOnlyList<RepairPartLineResponse>>> GetParts(int id, CancellationToken cancellationToken)
    {
        var request = await dbContext.RepairRequests.AsNoTracking()
            .Include(r => r.WarrantyCard)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request is null) return NotFound();
        if (!CanView(request)) return Forbid();

        var lines = await dbContext.RepairRequestParts.AsNoTracking()
            .Include(x => x.Part)
            .Where(x => x.RepairRequestId == id)
            .OrderBy(x => x.AddedAt)
            .Select(x => new RepairPartLineResponse(
                x.Id, x.PartId, x.Part.Code, x.Part.Name, x.Quantity, x.UnitPrice, x.Quantity * x.UnitPrice, x.AddedAt))
            .ToListAsync(cancellationToken);
        return Ok(lines);
    }

    [Authorize(Roles = "Admin,Manager,Technician")]
    [HttpPost("{id:int}/parts")]
    public async Task<ActionResult<RepairPartLineResponse>> AddPart(
        int id,
        AddRepairPartRequest body,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorId)) return Unauthorized();

        var request = await dbContext.RepairRequests
            .Include(r => r.WarrantyCard)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request is null) return NotFound();

        var isTech = User.IsInRole(nameof(UserRole.Technician));
        if (isTech && request.TechnicianId != actorId && !User.IsInRole(nameof(UserRole.Admin)) && !User.IsInRole(nameof(UserRole.Manager)))
            return Forbid();

        if (request.Status is RepairRequestStatus.Returned or RepairRequestStatus.Cancelled)
            return BadRequest(new ProblemDetails { Title = "Cannot add parts to a closed request." });

        var part = await dbContext.Parts.SingleOrDefaultAsync(p => p.Id == body.PartId && p.IsActive, cancellationToken);
        if (part is null)
            return BadRequest(new ProblemDetails { Title = "Part not found or inactive." });
        if (body.Quantity < 1)
            return BadRequest(new ProblemDetails { Title = "Quantity must be at least 1." });
        if (part.StockQuantity < body.Quantity)
            return BadRequest(new ProblemDetails { Title = "Insufficient stock." });

        part.StockQuantity -= body.Quantity;
        var line = new RepairRequestPart
        {
            RepairRequestId = id,
            PartId = part.Id,
            Quantity = body.Quantity,
            UnitPrice = part.UnitPrice,
        };
        dbContext.RepairRequestParts.Add(line);
        request.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new RepairPartLineResponse(
            line.Id, part.Id, part.Code, part.Name, line.Quantity, line.UnitPrice,
            line.Quantity * line.UnitPrice, line.AddedAt));
    }

    [Authorize(Roles = "Admin,Manager,Technician")]
    [HttpDelete("{id:int}/parts/{lineId:int}")]
    public async Task<IActionResult> RemovePart(int id, int lineId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorId)) return Unauthorized();
        var request = await dbContext.RepairRequests.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request is null) return NotFound();

        if (User.IsInRole(nameof(UserRole.Technician)) &&
            request.TechnicianId != actorId &&
            !User.IsInAnyRole(nameof(UserRole.Admin), nameof(UserRole.Manager)))
            return Forbid();

        var line = await dbContext.RepairRequestParts
            .Include(x => x.Part)
            .SingleOrDefaultAsync(x => x.Id == lineId && x.RepairRequestId == id, cancellationToken);
        if (line is null) return NotFound();

        if (line.Part is not null)
            line.Part.StockQuantity += line.Quantity;

        dbContext.RepairRequestParts.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool CanView(RepairRequest request)
    {
        if (User.IsInAnyRole(nameof(UserRole.Admin), nameof(UserRole.Manager), nameof(UserRole.Receptionist)))
            return true;

        if (!User.TryGetUserId(out var userId)) return false;

        if (User.IsInRole(nameof(UserRole.Customer)))
            return request.WarrantyCard.CustomerId == userId;

        if (User.IsInRole(nameof(UserRole.Technician)))
            return request.TechnicianId == userId || request.TechnicianId is null;

        return false;
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

public sealed class UpdateRepairRequestBody
{
    [System.ComponentModel.DataAnnotations.StringLength(4000)]
    public string? Description { get; init; }
}

public sealed class AddRepairPartRequest
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int PartId { get; init; }

    [System.ComponentModel.DataAnnotations.Range(1, 10000)]
    public int Quantity { get; init; }
}

public sealed record RepairPartLineResponse(
    int Id,
    int PartId,
    string PartCode,
    string PartName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    DateTime AddedAt);
