using Warranty.Enums;

namespace Warranty.Models;

public class RepairRequest
{
    public int Id { get; set; }
    public int WarrantyCardId { get; set; }
    public int? ReceptionistId { get; set; }
    public int? TechnicianId { get; set; }
    public required string Description { get; set; }
    public RepairRequestStatus Status { get; set; } = RepairRequestStatus.Received;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public WarrantyCard WarrantyCard { get; set; } = null!;
    public User? Receptionist { get; set; }
    public User? Technician { get; set; }
    public ICollection<RepairRequestStatusHistory> StatusHistory { get; set; } = new List<RepairRequestStatusHistory>();
}
