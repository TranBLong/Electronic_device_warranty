using Warranty.Enums;

namespace Warranty.Models;

public class WarrantyCard
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int CustomerId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public WarrantyStatus Status { get; set; } = WarrantyStatus.Active;

    public Product Product { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public ICollection<RepairRequest> RepairRequests { get; set; } = new List<RepairRequest>();
}
