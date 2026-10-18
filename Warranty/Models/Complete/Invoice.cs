using Warranty.Enums;

namespace Warranty.Models;

public class Invoice
{
    public int Id { get; set; }
    public int RepairRequestId { get; set; }
    public required string InvoiceNumber { get; set; }
    public decimal PartsTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public int IssuedById { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int? CancelledById { get; set; }
    public string? CancellationReason { get; set; }

    public RepairRequest RepairRequest { get; set; } = null!;
    public User IssuedBy { get; set; } = null!;
    public User? CancelledBy { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
