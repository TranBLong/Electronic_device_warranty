using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Services;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/warranty-cards")]
public sealed class WarrantyCardsController(IWarrantyCardService warrantyCardService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WarrantyCardResponse>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await warrantyCardService.GetAllAsync(userId, role, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WarrantyCardResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await warrantyCardService.GetByIdAsync(id, userId, role, cancellationToken));
    }

    [Authorize(Roles = nameof(UserRole.Receptionist))]
    [HttpPost]
    public async Task<ActionResult<WarrantyCardResponse>> Create(
        CreateWarrantyCardRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserRole(out var role))
        {
            return Unauthorized();
        }

        var card = await warrantyCardService.CreateAsync(request, role, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
    }

    private bool TryGetIdentity(out int userId, out UserRole role)
    {
        role = default;
        return User.TryGetUserId(out userId) && User.TryGetUserRole(out role);
    }
}
