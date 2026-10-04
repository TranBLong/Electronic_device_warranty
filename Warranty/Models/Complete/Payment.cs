using Warranty.Enums;

namespace Warranty.Models;

public class Payment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int ReceivedById { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? Note { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public Invoice Invoice { get; set; } = null!;
    public User ReceivedBy { get; set; } = null!;
}
