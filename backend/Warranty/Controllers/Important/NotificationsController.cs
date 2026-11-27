using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Services;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationResponse>>> GetMine(
        [FromQuery] QueryParameters query,
        [FromQuery] bool? unreadOnly,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var q = db.Notifications.AsNoTracking().Where(n => n.UserId == uid);
        if (unreadOnly == true) q = q.Where(n => !n.IsRead);

        q = q.OrderByDescending(n => n.CreatedAt);
        var total = await q.CountAsync(ct);
        var items = await q.Skip(query.Skip).Take(query.Take)
            .Select(n => new NotificationResponse(
                n.Id, n.Title, n.Message, n.Type.ToString(), n.IsRead, n.CreatedAt, n.RepairRequestId))
            .ToListAsync(ct);

        return Ok(PagedResult<NotificationResponse>.Create(items, query.PageNumber, query.Take, total));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> UnreadCount(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        var count = await db.Notifications.CountAsync(n => n.UserId == uid && !n.IsRead, ct);
        return Ok(new { count });
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        var n = await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct);
        if (n is null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        await db.Notifications
            .Where(n => n.UserId == uid && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
        return NoContent();
    }
}

public sealed record NotificationResponse(
    int Id, string Title, string Message, string Type, bool IsRead, DateTime CreatedAt, int? RepairRequestId);
