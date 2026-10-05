using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/service-centers")]
public sealed class ServiceCentersController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceCenterResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await dbContext.ServiceCenters.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new ServiceCenterResponse(c.Id, c.Name, c.Address, c.Phone, c.IsActive, c.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(items);
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
