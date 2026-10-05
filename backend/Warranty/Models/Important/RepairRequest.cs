using Warranty.Enums;

namespace Warranty.Models;

public class RepairRequest
{
    public int Id { get; set; }
    public int WarrantyCardId { get; set; }
    public int? ServiceCenterId { get; set; }
    public int? ReceptionistId { get; set; }
    public int? TechnicianId { get; set; }
    public required string Description { get; set; }
    public RepairRequestStatus Status { get; set; } = RepairRequestStatus.Received;
    public bool IsChargeable { get; set; }   // true nếu hết hạn / lỗi người dùng -> tính phí
    public decimal LaborCost { get; set; }   // tiền công
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public WarrantyCard WarrantyCard { get; set; } = null!;
    public ServiceCenter? ServiceCenter { get; set; }
    public User? Receptionist { get; set; }
    public User? Technician { get; set; }
    public Invoice? Invoice { get; set; }
    public Feedback? Feedback { get; set; }
    public ICollection<RepairRequestStatusHistory> StatusHistory { get; set; } = new List<RepairRequestStatusHistory>();
    public ICollection<RepairRequestPart> Parts { get; set; } = new List<RepairRequestPart>();
    public ICollection<RepairRequestAttachment> Attachments { get; set; } = new List<RepairRequestAttachment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}