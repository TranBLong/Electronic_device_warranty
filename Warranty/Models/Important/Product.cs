namespace Warranty.Models;

public class Product
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }
    public required string Name { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public required string SerialNumber { get; set; }
    public int WarrantyMonths { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Category? Category { get; set; }
    public ICollection<WarrantyCard> WarrantyCards { get; set; } = new List<WarrantyCard>();
}
