using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Services;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/repair-requests")]
public sealed class RepairRequestsController(IRepairRequestService repairRequestService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RepairRequestResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await repairRequestService.GetAllAsync(userId, role, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepairRequestResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await repairRequestService.GetByIdAsync(id, userId, role, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<RepairRequestResponse>> Create(
        CreateRepairRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var actorId, out var role))
        {
            return Unauthorized();
        }

        var repairRequest = await repairRequestService.CreateAsync(request, actorId, role, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = repairRequest.Id }, repairRequest);
    }

    [Authorize(Roles = "Receptionist,Manager")]
    [HttpPut("{id:int}/technician")]
    public async Task<ActionResult<RepairRequestResponse>> AssignTechnician(
        int id,
        AssignTechnicianRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserRole(out var role))
        {
            return Unauthorized();
        }

        return Ok(await repairRequestService.AssignTechnicianAsync(
            id, request.TechnicianId, role, cancellationToken));
    }

    [Authorize(Roles = nameof(UserRole.Technician))]
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<RepairRequestResponse>> ChangeStatus(
        int id,
        ChangeRepairStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var technicianId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await repairRequestService.ChangeStatusAsync(
            id, request.Status, technicianId, role, cancellationToken));
    }

    private bool TryGetIdentity(out int userId, out UserRole role)
    {
        role = default;
        return User.TryGetUserId(out userId) && User.TryGetUserRole(out role);
    }
}
