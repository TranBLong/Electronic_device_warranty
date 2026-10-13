using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Services;
using Warranty.Services.Interfaces.Complete;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class RepairRequestAttachmentsController(
    IRepairRequestAttachmentService attachmentService) : ControllerBase
{
    [Authorize(Roles = "Customer,Receptionist,Technician")]
    [HttpPost("repair-requests/{repairRequestId:int}/attachments")]
    public async Task<ActionResult<RepairRequestAttachmentResponseDto>> Upload(
        int repairRequestId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        var attachment = await attachmentService.UploadAsync(repairRequestId, file, userId, role, cancellationToken);
        return CreatedAtAction(nameof(GetByRepairRequestId), new { repairRequestId }, attachment);
    }

    [Authorize(Roles = "Admin,Manager,Receptionist,Technician,Customer")]
    [HttpGet("repair-requests/{repairRequestId:int}/attachments")]
    public async Task<ActionResult<IReadOnlyList<RepairRequestAttachmentResponseDto>>> GetByRepairRequestId(
        int repairRequestId,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await attachmentService.GetByRepairRequestIdAsync(
            repairRequestId, userId, role, cancellationToken));
    }

    [Authorize(Roles = "Admin,Manager,Receptionist,Technician,Customer")]
    [HttpGet("attachments/{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        var file = await attachmentService.GetDownloadAsync(id, userId, role, cancellationToken);
        return PhysicalFile(file.FilePath, file.ContentType, file.FileName);
    }

    private bool TryGetIdentity(out int userId, out UserRole role)
    {
        role = default;
        return User.TryGetUserId(out userId) && User.TryGetUserRole(out role);
    }
}
