using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Services;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/invoices")]
public sealed class InvoicesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.CanManageBilling)]
    public async Task<ActionResult<PagedResult<InvoiceResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var q = db.Invoices.AsNoTracking()
            .Include(i => i.RepairRequest).ThenInclude(r => r.WarrantyCard).ThenInclude(w => w.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var st))
            q = q.Where(i => i.Status == st);

        if (query.NormalizedKeyword is { } term)
            q = q.Where(i => i.InvoiceNumber.ToLower().Contains(term) ||
                             i.RepairRequest.WarrantyCard.Customer.FullName.ToLower().Contains(term));

        q = query.IsDescending
            ? q.OrderByDescending(i => i.IssuedAt)
            : q.OrderBy(i => i.IssuedAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip(query.Skip).Take(query.Take)
            .Select(i => ToResponse(i))
            .ToListAsync(ct);
        return Ok(PagedResult<InvoiceResponse>.Create(items, query.PageNumber, query.Take, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceResponse>> GetById(int id, CancellationToken ct)
    {
        var inv = await db.Invoices.AsNoTracking()
            .Include(i => i.RepairRequest).ThenInclude(r => r.WarrantyCard).ThenInclude(w => w.Customer)
            .Include(i => i.Payments)
            .SingleOrDefaultAsync(i => i.Id == id, ct);
        if (inv is null) return NotFound();

        if (User.IsInRole(Roles.Customer))
        {
            if (!User.TryGetUserId(out var uid) || inv.RepairRequest.WarrantyCard.CustomerId != uid)
                return Forbid();
        }
        return Ok(ToResponse(inv));
    }

    [HttpGet("my")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<IReadOnlyList<InvoiceResponse>>> GetMy(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        var items = await db.Invoices.AsNoTracking()
            .Include(i => i.RepairRequest).ThenInclude(r => r.WarrantyCard)
            .Where(i => i.RepairRequest.WarrantyCard.CustomerId == uid)
            .OrderByDescending(i => i.IssuedAt)
            .Select(i => ToResponse(i))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageBilling)]
    public async Task<ActionResult<InvoiceResponse>> Create([FromBody] CreateInvoiceRequest req, CancellationToken ct)
    {
        var rr = await db.RepairRequests
            .Include(r => r.Parts)
            .SingleOrDefaultAsync(r => r.Id == req.RepairRequestId, ct);
        if (rr is null) return NotFound(new ProblemDetails { Title = "Repair request not found." });

        if (await db.Invoices.AnyAsync(i => i.RepairRequestId == req.RepairRequestId, ct))
            return Conflict(new ProblemDetails { Title = "Invoice already exists for this repair request." });

        var partsTotal = req.PartsTotal >= 0 ? req.PartsTotal : rr.Parts.Sum(p => p.UnitPrice * p.Quantity);
        var labor = req.LaborTotal;
        var inv = new Invoice
        {
            RepairRequestId = rr.Id,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{rr.Id:D5}",
            PartsTotal = partsTotal,
            LaborTotal = labor,
            TotalAmount = partsTotal + labor,
            Status = InvoiceStatus.Unpaid,
            IssuedAt = DateTime.UtcNow
        };
        db.Invoices.Add(inv);
        await db.SaveChangesAsync(ct);

        inv = await db.Invoices.AsNoTracking()
            .Include(i => i.RepairRequest).ThenInclude(r => r.WarrantyCard).ThenInclude(w => w.Customer)
            .SingleAsync(i => i.Id == inv.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = inv.Id }, ToResponse(inv));
    }

    private static InvoiceResponse ToResponse(Invoice i) => new(
        i.Id,
        i.InvoiceNumber,
        i.RepairRequestId,
        i.PartsTotal,
        i.LaborTotal,
        i.TotalAmount,
        i.Status.ToString(),
        i.IssuedAt,
        i.PaidAt,
        i.RepairRequest?.WarrantyCard?.Customer?.FullName);
}

public sealed class CreateInvoiceRequest
{
    public int RepairRequestId { get; init; }
    public decimal PartsTotal { get; init; } = -1;
    public decimal LaborTotal { get; init; }
}

public sealed record InvoiceResponse(
    int Id, string InvoiceNumber, int RepairRequestId,
    decimal PartsTotal, decimal LaborTotal, decimal TotalAmount,
    string Status, DateTime IssuedAt, DateTime? PaidAt, string? CustomerName);
