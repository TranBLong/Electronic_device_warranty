using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoryResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        CancellationToken cancellationToken)
    {
        var q = dbContext.Categories.AsNoTracking().Where(c => c.IsActive).AsQueryable();
        if (query.NormalizedKeyword is { } term)
            q = q.Where(c => c.Name.ToLower().Contains(term) ||
                             (c.Description != null && c.Description.ToLower().Contains(term)));
        q = query.IsDescending ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name);
        var total = await q.CountAsync(cancellationToken);
        var items = await q.Skip(query.Skip).Take(query.Take)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(PagedResult<CategoryResponse>.Create(items, query.PageNumber, query.Take, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null) return NotFound();
        return Ok(new CategoryResponse(category.Id, category.Name, category.Description, category.IsActive, category.CreatedAt));
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageCatalog)]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true
        };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            new CategoryResponse(category.Id, category.Name, category.Description, category.IsActive, category.CreatedAt));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.CanManageCatalog)]
    public async Task<ActionResult<CategoryResponse>> Update(
        int id,
        [FromBody] CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null) return NotFound();
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new CategoryResponse(category.Id, category.Name, category.Description, category.IsActive, category.CreatedAt));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.CanManageCatalog)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null) return NotFound();
        category.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed class CategoryRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(150)]
    public string Name { get; init; } = string.Empty;
    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? Description { get; init; }
}

public sealed record CategoryResponse(int Id, string Name, string? Description, bool IsActive, DateTime CreatedAt);
