namespace Warranty.Models;

public class Part
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public required string PartNumber { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Category Category { get; set; } = null!;
    public ICollection<RepairRequestPart> RepairRequestParts { get; set; } = new List<RepairRequestPart>();
}