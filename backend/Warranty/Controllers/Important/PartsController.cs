using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/parts")]
public sealed class PartsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Technician,Receptionist")]
    public async Task<ActionResult<IReadOnlyList<PartResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? inStockOnly,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Parts.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Code.ToLower().Contains(term));
        }
        if (inStockOnly == true)
        {
            query = query.Where(p => p.StockQuantity > 0);
        }

        var items = await query.OrderBy(p => p.Name)
            .Select(p => new PartResponse(p.Id, p.Code, p.Name, p.Description, p.UnitPrice, p.StockQuantity, p.IsActive, p.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Manager,Technician,Receptionist")]
    public async Task<ActionResult<PartResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var part = await dbContext.Parts.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (part is null) return NotFound();
        return Ok(new PartResponse(part.Id, part.Code, part.Name, part.Description, part.UnitPrice, part.StockQuantity, part.IsActive, part.CreatedAt));
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<PartResponse>> Create([FromBody] PartRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.Parts.AnyAsync(p => p.Code == request.Code.Trim(), cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Part code already exists." });
        }

        var part = new Part
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            UnitPrice = request.UnitPrice,
            StockQuantity = request.StockQuantity,
            IsActive = true
        };
        dbContext.Parts.Add(part);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = part.Id },
            new PartResponse(part.Id, part.Code, part.Name, part.Description, part.UnitPrice, part.StockQuantity, part.IsActive, part.CreatedAt));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<PartResponse>> Update(int id, [FromBody] PartRequest request, CancellationToken cancellationToken)
    {
        var part = await dbContext.Parts.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (part is null) return NotFound();

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Parts.AnyAsync(p => p.Code == code && p.Id != id, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Part code already exists." });
        }

        part.Code = code;
        part.Name = request.Name.Trim();
        part.Description = request.Description?.Trim();
        part.UnitPrice = request.UnitPrice;
        part.StockQuantity = request.StockQuantity;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new PartResponse(part.Id, part.Code, part.Name, part.Description, part.UnitPrice, part.StockQuantity, part.IsActive, part.CreatedAt));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var part = await dbContext.Parts.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (part is null) return NotFound();
        part.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed class PartRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(50)]
    public string Code { get; init; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(200)]
    public string Name { get; init; } = string.Empty;
    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? Description { get; init; }
    [System.ComponentModel.DataAnnotations.Range(0, 999999999)]
    public decimal UnitPrice { get; init; }
    [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
    public int StockQuantity { get; init; }
}

public sealed record PartResponse(
    int Id, string Code, string Name, string? Description,
    decimal UnitPrice, int StockQuantity, bool IsActive, DateTime CreatedAt);
