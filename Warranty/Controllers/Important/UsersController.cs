using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Services;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await userService.GetMeAsync(userId, cancellationToken));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateMe(
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await userService.UpdateMeAsync(userId, request, cancellationToken));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeactivateMe(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await userService.DeactivateMeAsync(userId, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Receptionist")]
    [HttpGet("customers")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetCustomers(CancellationToken cancellationToken) =>
        Ok(await userService.GetCustomersAsync(cancellationToken));

    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpGet("technicians")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetTechnicians(CancellationToken cancellationToken) =>
        Ok(await userService.GetTechniciansAsync(cancellationToken));

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await userService.GetAllAsync(cancellationToken));

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await userService.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(
        AdminCreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(
        int id,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await userService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
