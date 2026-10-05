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
    public async Task<ActionResult<IReadOnlyList<WarrantyCardResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var cards = dbContext.WarrantyCards.AsNoTracking()
            .Include(card => card.Product)
            .Include(card => card.Customer)
            .AsQueryable();

        if (User.IsInRole(nameof(UserRole.Customer)))
        {
            if (!User.TryGetUserId(out var customerId))
            {
                return Unauthorized();
            }

            cards = cards.Where(card => card.CustomerId == customerId);
        }
        else if (!User.IsInRole(nameof(UserRole.Admin)) &&
                 !User.IsInRole(nameof(UserRole.Receptionist)))
        {
            return Forbid();
        }

        return Ok(await cards.OrderByDescending(card => card.StartDate)
            .Select(card => ToResponse(card))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WarrantyCardResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var card = await dbContext.WarrantyCards.AsNoTracking()
            .Include(candidate => candidate.Product)
            .Include(candidate => candidate.Customer)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (card is null)
        {
            return NotFound();
        }

        if (User.IsInRole(nameof(UserRole.Customer)))
        {
            if (!User.TryGetUserId(out var customerId))
            {
                return Unauthorized();
            }

            if (card.CustomerId != customerId)
            {
                return Forbid();
            }
        }
        else if (!User.IsInRole(nameof(UserRole.Admin)) &&
                 !User.IsInRole(nameof(UserRole.Receptionist)))
        {
            return Forbid();
        }

        return Ok(ToResponse(card));
    }

    [Authorize(Roles = nameof(UserRole.Receptionist))]
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
            candidate => candidate.Id == request.ProductId,
            cancellationToken);
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
