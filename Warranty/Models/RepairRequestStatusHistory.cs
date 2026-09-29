using Warranty.Enums;

namespace Warranty.Models;

public class RepairRequestStatusHistory
{
    public int Id { get; set; }
    public int RepairRequestId { get; set; }
    public RepairRequestStatus? OldStatus { get; set; }
    public RepairRequestStatus NewStatus { get; set; }
    public int ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public RepairRequest RepairRequest { get; set; } = null!;
    public User User { get; set; } = null!;
}
