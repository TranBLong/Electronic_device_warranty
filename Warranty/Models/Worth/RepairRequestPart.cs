namespace Warranty.Models;

public class RepairRequestPart
{
    public int Id { get; set; }
    public int RepairRequestId { get; set; }
    public int PartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; } // giá tại thời điểm sửa, không phụ thuộc Part.UnitPrice sau này
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public RepairRequest RepairRequest { get; set; } = null!;
    public Part Part { get; set; } = null!;
}