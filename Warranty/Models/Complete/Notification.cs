using Warranty.Enums;

namespace Warranty.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? RepairRequestId { get; set; }
    public NotificationType Type { get; set; } = NotificationType.General;
    public required string Title { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public RepairRequest? RepairRequest { get; set; }
}
