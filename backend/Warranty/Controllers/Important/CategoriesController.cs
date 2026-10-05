using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Categories.AsNoTracking().Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term));
        }

        var items = await query.OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(items);
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
    [Authorize(Roles = nameof(UserRole.Admin))]
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
    [Authorize(Roles = nameof(UserRole.Admin))]
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
    [Authorize(Roles = nameof(UserRole.Admin))]
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
