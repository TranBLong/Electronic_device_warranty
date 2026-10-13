using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Services;
using Warranty.Services.Interfaces.Worth;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/repair-requests/{repairRequestId:int}/parts")]
public sealed class RepairRequestPartsController(IRepairRequestPartService repairRequestPartService) : ControllerBase
{
    [Authorize(Roles = "Admin,Manager,Receptionist,Technician,Customer")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RepairRequestPartResponseDto>>> GetByRepairRequestId(
        int repairRequestId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId) || !User.TryGetUserRole(out var role))
        {
            return Unauthorized();
        }

        return Ok(await repairRequestPartService.GetByRepairRequestIdAsync(
            repairRequestId, userId, role, cancellationToken));
    }

    [Authorize(Roles = nameof(UserRole.Technician))]
    [HttpPost]
    public async Task<ActionResult<RepairRequestPartResponseDto>> Add(
        int repairRequestId,
        RepairRequestPartCreateDto request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var technicianId) || !User.TryGetUserRole(out var role))
        {
            return Unauthorized();
        }

        var usage = await repairRequestPartService.AddAsync(
            repairRequestId, request, technicianId, role, cancellationToken);
        return CreatedAtAction(nameof(GetByRepairRequestId), new { repairRequestId }, usage);
    }
}
