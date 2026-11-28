using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Services;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController(AppDbContext db, NotificationService notifications) : ControllerBase
{
    /// <summary>Lists payments recorded for an invoice.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.CanManageBilling)]
    public async Task<ActionResult<IReadOnlyList<PaymentResponse>>> GetByInvoice(
        [FromQuery] int invoiceId, CancellationToken ct)
    {
        var items = await db.Payments.AsNoTracking()
            .Include(p => p.ReceivedBy)
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PaymentResponse(
                p.Id, p.InvoiceId, p.Amount, p.Method.ToString(), p.Note, p.PaidAt,
                p.ReceivedBy.FullName))
            .ToListAsync(ct);
        return Ok(items);
    }

    /// <summary>Records a payment against an invoice.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.CanManageBilling)]
    public async Task<ActionResult<PaymentResponse>> Create(
        [FromBody] CreatePaymentRequest req, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var staffId)) return Unauthorized();

        var invoice = await db.Invoices
            .Include(i => i.Payments)
            .Include(i => i.RepairRequest).ThenInclude(r => r.WarrantyCard)
            .SingleOrDefaultAsync(i => i.Id == req.InvoiceId, ct);
        if (invoice is null) return NotFound();

        if (req.Amount <= 0)
            return BadRequest(new ProblemDetails { Title = "Amount must be positive." });

        var paidSoFar = invoice.Payments.Sum(p => p.Amount);
        if (paidSoFar + req.Amount > invoice.TotalAmount + 0.01m)
            return BadRequest(new ProblemDetails { Title = "Payment exceeds invoice total." });

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            ReceivedById = staffId,
            Amount = req.Amount,
            Method = req.Method,
            Note = req.Note?.Trim(),
            PaidAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);

        var newTotal = paidSoFar + req.Amount;
        if (newTotal >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }
        else
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        await db.SaveChangesAsync(ct);

        var customerId = invoice.RepairRequest.WarrantyCard.CustomerId;
        await notifications.NotifyAsync(
            customerId,
            "Thanh toán đã ghi nhận",
            $"Hóa đơn {invoice.InvoiceNumber}: đã nhận {req.Amount:N0} đ qua {req.Method}.",
            NotificationType.Payment,
            invoice.RepairRequestId,
            ct);

        var staff = await db.Users.AsNoTracking().SingleAsync(u => u.Id == staffId, ct);
        return CreatedAtAction(nameof(GetByInvoice), new { invoiceId = invoice.Id },
            new PaymentResponse(payment.Id, payment.InvoiceId, payment.Amount,
                payment.Method.ToString(), payment.Note, payment.PaidAt, staff.FullName));
    }
}

public sealed class CreatePaymentRequest
{
    public int InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; } = PaymentMethod.Cash;
    public string? Note { get; init; }
}

public sealed record PaymentResponse(
    int Id, int InvoiceId, decimal Amount, string Method, string? Note,
    DateTime PaidAt, string ReceivedByName);
