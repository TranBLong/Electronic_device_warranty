using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Services;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/feedbacks")]
public sealed class FeedbacksController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists customer feedback.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminOrManager)]
    public async Task<ActionResult<IReadOnlyList<FeedbackResponse>>> GetAll(CancellationToken ct)
    {
        var items = await db.Feedbacks.AsNoTracking()
            .Include(f => f.Customer)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackResponse(
                f.Id, f.RepairRequestId, f.CustomerId, f.Customer.FullName,
                f.Rating, f.Comment, f.CreatedAt))
            .ToListAsync(ct);
        return Ok(items);
    }

    /// <summary>Gets feedback for a repair request.</summary>
    [HttpGet("by-repair/{repairRequestId:int}")]
    public async Task<ActionResult<FeedbackResponse>> GetByRepair(int repairRequestId, CancellationToken ct)
    {
        var f = await db.Feedbacks.AsNoTracking()
            .Include(x => x.Customer)
            .SingleOrDefaultAsync(x => x.RepairRequestId == repairRequestId, ct);
        if (f is null) return NotFound();
        return Ok(new FeedbackResponse(f.Id, f.RepairRequestId, f.CustomerId, f.Customer.FullName,
            f.Rating, f.Comment, f.CreatedAt));
    }

    /// <summary>Submits customer feedback for a completed repair request.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<FeedbackResponse>> Create(
        [FromBody] CreateFeedbackRequest req, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        if (req.Rating is < 1 or > 5)
            return BadRequest(new ProblemDetails { Title = "Rating must be 1–5." });

        var rr = await db.RepairRequests
            .Include(r => r.WarrantyCard)
            .SingleOrDefaultAsync(r => r.Id == req.RepairRequestId, ct);
        if (rr is null) return NotFound();
        if (rr.WarrantyCard.CustomerId != uid) return Forbid();
        if (rr.Status is not (RepairRequestStatus.Completed or RepairRequestStatus.Returned))
            return BadRequest(new ProblemDetails { Title = "Chỉ đánh giá khi phiếu đã hoàn tất / trả máy." });

        if (await db.Feedbacks.AnyAsync(f => f.RepairRequestId == req.RepairRequestId, ct))
            return Conflict(new ProblemDetails { Title = "Already submitted feedback." });

        var feedback = new Feedback
        {
            RepairRequestId = req.RepairRequestId,
            CustomerId = uid,
            Rating = req.Rating,
            Comment = req.Comment?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        db.Feedbacks.Add(feedback);
        await db.SaveChangesAsync(ct);

        var me = await db.Users.AsNoTracking().SingleAsync(u => u.Id == uid, ct);
        return CreatedAtAction(nameof(GetByRepair), new { repairRequestId = feedback.RepairRequestId },
            new FeedbackResponse(feedback.Id, feedback.RepairRequestId, feedback.CustomerId,
                me.FullName, feedback.Rating, feedback.Comment, feedback.CreatedAt));
    }
}

public sealed class CreateFeedbackRequest
{
    public int RepairRequestId { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
}

public sealed record FeedbackResponse(
    int Id, int RepairRequestId, int CustomerId, string CustomerName,
    int Rating, string? Comment, DateTime CreatedAt);
