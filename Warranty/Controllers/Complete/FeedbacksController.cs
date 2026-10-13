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
public sealed class FeedbacksController(IFeedbackService feedbackService) : ControllerBase
{
    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpPost("repair-requests/{repairRequestId:int}/feedback")]
    public async Task<ActionResult<FeedbackResponseDto>> Create(
        int repairRequestId,
        FeedbackCreateDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        var feedback = await feedbackService.CreateAsync(repairRequestId, request, userId, role, cancellationToken);
        return CreatedAtAction(nameof(GetByRepairRequestId), new { repairRequestId }, feedback);
    }

    [Authorize(Roles = "Admin,Manager,Customer")]
    [HttpGet("repair-requests/{repairRequestId:int}/feedback")]
    public async Task<ActionResult<FeedbackResponseDto>> GetByRepairRequestId(
        int repairRequestId,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await feedbackService.GetByRepairRequestIdAsync(repairRequestId, userId, role, cancellationToken));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("feedbacks")]
    public async Task<ActionResult<PagedResponseDto<FeedbackResponseDto>>> GetAll(
        [FromQuery] int? rating,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await feedbackService.GetAllAsync(rating, page, pageSize, cancellationToken));

    private bool TryGetIdentity(out int userId, out UserRole role)
    {
        role = default;
        return User.TryGetUserId(out userId) && User.TryGetUserRole(out role);
    }
}
