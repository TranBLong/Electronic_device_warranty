using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Services.Interfaces.Complete;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/service-centers")]
public sealed class ServiceCentersController(IServiceCenterService serviceCenterService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceCenterResponseDto>>> GetAll(
        [FromQuery] string? name,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        Ok(await serviceCenterService.GetAllAsync(name, isActive, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceCenterResponseDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await serviceCenterService.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<ServiceCenterResponseDto>> Create(
        ServiceCenterCreateDto request,
        CancellationToken cancellationToken)
    {
        var center = await serviceCenterService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = center.Id }, center);
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceCenterResponseDto>> Update(
        int id,
        ServiceCenterUpdateDto request,
        CancellationToken cancellationToken) =>
        Ok(await serviceCenterService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPatch("{id:int}/active")]
    public async Task<ActionResult<ServiceCenterResponseDto>> SetActive(
        int id,
        [FromBody] ServiceCenterActiveRequest request,
        CancellationToken cancellationToken) =>
        Ok(await serviceCenterService.SetActiveAsync(id, request.IsActive, cancellationToken));

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await serviceCenterService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
