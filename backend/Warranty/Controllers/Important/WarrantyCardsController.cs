using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Services;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/warranty-cards")]
public sealed class WarrantyCardsController(
    AppDbContext dbContext,
    IWarrantyCardService warrantyCardService) : ControllerBase
{
    /// <summary>Lists warranty cards visible to the current user.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<WarrantyCardResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var cards = dbContext.WarrantyCards.AsNoTracking()
            .Include(card => card.Product)
            .Include(card => card.Customer)
            .AsQueryable();

        if (User.IsInRole(nameof(UserRole.Customer)))
        {
            if (!User.TryGetUserId(out var customerId)) return Unauthorized();
            cards = cards.Where(card => card.CustomerId == customerId);
        }
        else if (!User.IsInAnyRole(
                     nameof(UserRole.Admin),
                     nameof(UserRole.Manager),
                     nameof(UserRole.Receptionist),
                     nameof(UserRole.Technician)))
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<WarrantyStatus>(status, true, out var statusFilter))
        {
            cards = cards.Where(c => c.Status == statusFilter);
        }

        if (query.NormalizedKeyword is { } term)
        {
            cards = cards.Where(c =>
                c.Product.Name.ToLower().Contains(term) ||
                c.Product.SerialNumber.ToLower().Contains(term) ||
                c.Customer.FullName.ToLower().Contains(term));
        }

        cards = (query.SortBy?.ToLowerInvariant()) switch
        {
            "product" or "productname" => query.IsDescending
                ? cards.OrderByDescending(c => c.Product.Name).ThenByDescending(c => c.Id)
                : cards.OrderBy(c => c.Product.Name).ThenBy(c => c.Id),
            "customer" or "customername" => query.IsDescending
                ? cards.OrderByDescending(c => c.Customer.FullName).ThenByDescending(c => c.Id)
                : cards.OrderBy(c => c.Customer.FullName).ThenBy(c => c.Id),
            "enddate" => query.IsDescending
                ? cards.OrderByDescending(c => c.EndDate).ThenByDescending(c => c.Id)
                : cards.OrderBy(c => c.EndDate).ThenBy(c => c.Id),
            "status" => query.IsDescending
                ? cards.OrderByDescending(c => c.Status).ThenByDescending(c => c.Id)
                : cards.OrderBy(c => c.Status).ThenBy(c => c.Id),
            "id" => query.IsDescending ? cards.OrderByDescending(c => c.Id) : cards.OrderBy(c => c.Id),
            _ => query.IsDescending
                ? cards.OrderByDescending(c => c.StartDate).ThenByDescending(c => c.Id)
                : cards.OrderBy(c => c.StartDate).ThenBy(c => c.Id)
        };

        var total = await cards.CountAsync(cancellationToken);
        var items = await cards
            .Skip(query.Skip)
            .Take(query.Take)
            .Select(card => WarrantyCardMapping.ToResponse(card))
            .ToListAsync(cancellationToken);
        return Ok(PagedResult<WarrantyCardResponse>.Create(items, query.PageNumber, query.Take, total));
    }

    /// <summary>Gets a warranty card by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<WarrantyCardResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var card = await dbContext.WarrantyCards.AsNoTracking()
            .Include(candidate => candidate.Product)
            .Include(candidate => candidate.Customer)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (card is null) return NotFound();

        if (User.IsInRole(nameof(UserRole.Customer)))
        {
            if (!User.TryGetUserId(out var customerId)) return Unauthorized();
            if (card.CustomerId != customerId) return Forbid();
        }
        else if (!User.IsInAnyRole(
                     nameof(UserRole.Admin),
                     nameof(UserRole.Manager),
                     nameof(UserRole.Receptionist),
                     nameof(UserRole.Technician)))
        {
            return Forbid();
        }

        return Ok(WarrantyCardMapping.ToResponse(card));
    }

    /// <summary>Looks up warranty status by product serial number.</summary>
    [AllowAnonymous]
    [HttpGet("by-serial/{serialNumber}")]
    public async Task<ActionResult<WarrantyCardLookupResponse>> LookupBySerial(
        string serialNumber,
        CancellationToken cancellationToken)
    {
        var normalizedSerialNumber = serialNumber.Trim().ToUpperInvariant();
        var card = await dbContext.WarrantyCards.AsNoTracking()
            .Include(candidate => candidate.Product)
            .SingleOrDefaultAsync(
                candidate => candidate.Product.SerialNumber == normalizedSerialNumber,
                cancellationToken);
        if (card is null) return NotFound();

        var status = card.Status == WarrantyStatus.Active && card.EndDate.Date < DateTime.UtcNow.Date
            ? WarrantyStatus.Expired
            : card.Status;
        return Ok(new WarrantyCardLookupResponse(
            card.Id,
            card.ProductId,
            card.Product.Name,
            card.Product.SerialNumber,
            card.StartDate,
            card.EndDate,
            status));
    }

    /// <summary>Creates a warranty card.</summary>
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpPost]
    public async Task<ActionResult<WarrantyCardResponse>> Create(
        CreateWarrantyCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await warrantyCardService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess) return ServiceError<WarrantyCardResponse>(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Updates a warranty card.</summary>
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<WarrantyCardResponse>> Update(
        int id,
        UpdateWarrantyCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await warrantyCardService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ServiceError<WarrantyCardResponse>(result);
    }

    /// <summary>Voids or deletes a warranty card.</summary>
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> VoidOrDelete(int id, CancellationToken cancellationToken)
    {
        var card = await dbContext.WarrantyCards.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (card is null) return NotFound();

        if (await dbContext.RepairRequests.AnyAsync(r => r.WarrantyCardId == id, cancellationToken))
        {
            // Soft: void instead of hard delete when referenced
            card.Status = WarrantyStatus.Voided;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Card voided because it has repair requests." });
        }

        dbContext.WarrantyCards.Remove(card);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private ActionResult<T> ServiceError<T>(ServiceResult<T> result) =>
        result.ErrorCode switch
        {
            ServiceErrorCode.NotFound => NotFound(),
            ServiceErrorCode.Forbidden => Forbid(),
            ServiceErrorCode.Invalid => BadRequest(new ProblemDetails { Title = result.ErrorMessage }),
            _ => throw new InvalidOperationException("A successful result cannot be mapped to an error response.")
        };
}
