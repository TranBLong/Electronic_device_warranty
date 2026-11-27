using Warranty.Data;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Services;

public sealed class NotificationService(AppDbContext db, ILogger<NotificationService> logger)
{
    public async Task NotifyAsync(
        int userId,
        string title,
        string message,
        NotificationType type = NotificationType.General,
        int? repairRequestId = null,
        CancellationToken ct = default)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RepairRequestId = repairRequestId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Notification to user {UserId}: {Title}", userId, title);
    }
}
