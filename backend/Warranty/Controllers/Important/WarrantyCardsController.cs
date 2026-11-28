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
[Route("api/warranty-cards")]
public sealed class WarrantyCardsController(AppDbContext dbContext) : ControllerBase
{
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
            .Select(card => ToResponse(card))
            .ToListAsync(cancellationToken);
        return Ok(PagedResult<WarrantyCardResponse>.Create(items, query.PageNumber, query.Take, total));
    }

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

        return Ok(ToResponse(card));
    }

    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpPost]
    public async Task<ActionResult<WarrantyCardResponse>> Create(
        CreateWarrantyCardRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StartDate == default)
        {
            return BadRequest(new ProblemDetails { Title = "Start date is required." });
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(
            candidate => candidate.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            return BadRequest(new ProblemDetails { Title = "Product was not found." });
        }

        var customer = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == request.CustomerId && candidate.Role == UserRole.Customer && candidate.IsActive,
            cancellationToken);
        if (customer is null)
        {
            return BadRequest(new ProblemDetails { Title = "An active Customer account is required." });
        }

        var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        var card = new WarrantyCard
        {
            ProductId = product.Id,
            CustomerId = customer.Id,
            StartDate = startDate,
            EndDate = startDate.AddMonths(product.WarrantyMonths),
            Status = WarrantyStatus.Active
        };
        dbContext.WarrantyCards.Add(card);
        await dbContext.SaveChangesAsync(cancellationToken);

        card.Product = product;
        card.Customer = customer;
        return CreatedAtAction(nameof(GetById), new { id = card.Id }, ToResponse(card));
    }

    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<WarrantyCardResponse>> Update(
        int id,
        UpdateWarrantyCardRequest request,
        CancellationToken cancellationToken)
    {
        var card = await dbContext.WarrantyCards
            .Include(c => c.Product)
            .Include(c => c.Customer)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (card is null) return NotFound();

        if (request.Status is not null)
        {
            if (!Enum.IsDefined(request.Status.Value))
            {
                return BadRequest(new ProblemDetails { Title = "Invalid warranty status." });
            }
            card.Status = request.Status.Value;
        }

        if (request.StartDate is not null)
        {
            card.StartDate = DateTime.SpecifyKind(request.StartDate.Value.Date, DateTimeKind.Utc);
            card.EndDate = card.StartDate.AddMonths(card.Product.WarrantyMonths);
        }

        if (request.EndDate is not null)
        {
            var end = DateTime.SpecifyKind(request.EndDate.Value.Date, DateTimeKind.Utc);
            if (end < card.StartDate)
            {
                return BadRequest(new ProblemDetails { Title = "End date must be on or after start date." });
            }
            card.EndDate = end;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(card));
    }

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

    private static WarrantyCardResponse ToResponse(WarrantyCard card) => new(
        card.Id,
        card.ProductId,
        card.Product.Name,
        card.Product.SerialNumber,
        card.CustomerId,
        card.Customer.FullName,
        card.StartDate,
        card.EndDate,
        card.Status == WarrantyStatus.Active && card.EndDate.Date < DateTime.UtcNow.Date
            ? WarrantyStatus.Expired
            : card.Status);
}

public sealed class UpdateWarrantyCardRequest
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public WarrantyStatus? Status { get; init; }
}
