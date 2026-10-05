namespace Warranty.Models;

public class Feedback
{
    public int Id { get; set; }
    public int RepairRequestId { get; set; }
    public int CustomerId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public RepairRequest RepairRequest { get; set; } = null!;
    public User Customer { get; set; } = null!;
}
