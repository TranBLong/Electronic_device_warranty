using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Services.Interfaces.Worth;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponseDto>>> GetAll(
        [FromQuery] string? name,
        CancellationToken cancellationToken) =>
        Ok(await categoryService.GetAllAsync(name, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponseDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await categoryService.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> Create(
        CategoryCreateDto request,
        CancellationToken cancellationToken)
    {
        var category = await categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponseDto>> Update(
        int id,
        CategoryUpdateDto request,
        CancellationToken cancellationToken) =>
        Ok(await categoryService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
