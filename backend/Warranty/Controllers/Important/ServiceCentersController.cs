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
[Route("api/service-centers")]
public sealed class ServiceCentersController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceCenterResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        CancellationToken cancellationToken)
    {
        var centers = dbContext.ServiceCenters.AsNoTracking()
            .Where(center => center.IsActive)
            .AsQueryable();
        if (query.NormalizedKeyword is { } term)
            centers = centers.Where(center =>
                center.Name.ToLower().Contains(term) ||
                center.Address.ToLower().Contains(term) ||
                (center.Phone != null && center.Phone.ToLower().Contains(term)));

        centers = (query.SortBy?.ToLowerInvariant()) switch
        {
            "address" => query.IsDescending
                ? centers.OrderByDescending(center => center.Address).ThenByDescending(center => center.Id)
                : centers.OrderBy(center => center.Address).ThenBy(center => center.Id),
            "created" or "createdat" => query.IsDescending
                ? centers.OrderByDescending(center => center.CreatedAt).ThenByDescending(center => center.Id)
                : centers.OrderBy(center => center.CreatedAt).ThenBy(center => center.Id),
            "id" => query.IsDescending
                ? centers.OrderByDescending(center => center.Id)
                : centers.OrderBy(center => center.Id),
            _ => query.IsDescending
                ? centers.OrderByDescending(center => center.Name).ThenByDescending(center => center.Id)
                : centers.OrderBy(center => center.Name).ThenBy(center => center.Id)
        };

        var total = await centers.CountAsync(cancellationToken);
        var items = await centers.Skip(query.Skip).Take(query.Take)
            .Select(c => new ServiceCenterResponse(c.Id, c.Name, c.Address, c.Phone, c.IsActive, c.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(PagedResult<ServiceCenterResponse>.Create(items, query.PageNumber, query.Take, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceCenterResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var center = await dbContext.ServiceCenters.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (center is null) return NotFound();
        return Ok(new ServiceCenterResponse(center.Id, center.Name, center.Address, center.Phone, center.IsActive, center.CreatedAt));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ServiceCenterResponse>> Create(
        [FromBody] ServiceCenterRequest request,
        CancellationToken cancellationToken)
    {
        var center = new ServiceCenter
        {
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            Phone = request.Phone?.Trim(),
            IsActive = true
        };
        dbContext.ServiceCenters.Add(center);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = center.Id },
            new ServiceCenterResponse(center.Id, center.Name, center.Address, center.Phone, center.IsActive, center.CreatedAt));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ServiceCenterResponse>> Update(
        int id,
        [FromBody] ServiceCenterRequest request,
        CancellationToken cancellationToken)
    {
        var center = await dbContext.ServiceCenters.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (center is null) return NotFound();
        center.Name = request.Name.Trim();
        center.Address = request.Address.Trim();
        center.Phone = request.Phone?.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new ServiceCenterResponse(center.Id, center.Name, center.Address, center.Phone, center.IsActive, center.CreatedAt));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var center = await dbContext.ServiceCenters.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (center is null) return NotFound();
        center.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed class ServiceCenterRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(200)]
    public string Name { get; init; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(500)]
    public string Address { get; init; } = string.Empty;
    [System.ComponentModel.DataAnnotations.StringLength(30)]
    public string? Phone { get; init; }
}

public sealed record ServiceCenterResponse(
    int Id, string Name, string Address, string? Phone, bool IsActive, DateTime CreatedAt);
