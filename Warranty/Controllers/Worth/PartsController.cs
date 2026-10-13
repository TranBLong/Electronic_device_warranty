using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Services.Interfaces.Worth;

namespace Warranty.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager,Receptionist,Technician")]
[Route("api/parts")]
public sealed class PartsController(IPartService partService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PartListResponseDto>> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await partService.GetAllAsync(categoryId, search, page, pageSize, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PartResponseDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await partService.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<PartResponseDto>> Create(
        PartCreateDto request,
        CancellationToken cancellationToken)
    {
        var part = await partService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = part.Id }, part);
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PartResponseDto>> Update(
        int id,
        PartUpdateDto request,
        CancellationToken cancellationToken) =>
        Ok(await partService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPatch("{id:int}/stock")]
    public async Task<ActionResult<PartResponseDto>> UpdateStock(
        int id,
        PartStockUpdateDto request,
        CancellationToken cancellationToken) =>
        Ok(await partService.UpdateStockAsync(id, request, cancellationToken));

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await partService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
