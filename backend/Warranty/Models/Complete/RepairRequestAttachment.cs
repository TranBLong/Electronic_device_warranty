namespace Warranty.Models;

public class RepairRequestAttachment
{
    public int Id { get; set; }
    public int RepairRequestId { get; set; }
    public int UploadedById { get; set; }
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public required string ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public RepairRequest RepairRequest { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}
