using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;

namespace Warranty.Repositories.Implementations.Complete;

public sealed class InvoiceRepository(AppDbContext dbContext) : IInvoiceRepository
{
    public Task<Invoice?> GetByIdAsync(
        int id,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Invoices.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return WithDetails(query).SingleOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);
    }

    public Task<Invoice?> GetByRepairRequestIdAsync(
        int repairRequestId,
        CancellationToken cancellationToken = default) =>
        WithDetails(dbContext.Invoices.AsNoTracking())
            .SingleOrDefaultAsync(
                invoice => invoice.RepairRequestId == repairRequestId && invoice.Status != InvoiceStatus.Cancelled,
                cancellationToken);

    public async Task<IReadOnlyList<Invoice>> GetAllAsync(
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = WithDetails(dbContext.Invoices.AsNoTracking());
        if (status.HasValue)
        {
            query = query.Where(invoice => invoice.Status == status.Value);
        }

        return await query.OrderByDescending(invoice => invoice.IssuedAt)
            .ThenByDescending(invoice => invoice.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetForCustomerAsync(
        int customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        await WithDetails(dbContext.Invoices.AsNoTracking())
            .Where(invoice => invoice.RepairRequest.WarrantyCard.CustomerId == customerId)
            .OrderByDescending(invoice => invoice.IssuedAt)
            .ThenByDescending(invoice => invoice.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(
        InvoiceStatus? status,
        int? customerId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Invoices.AsNoTracking().AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(invoice => invoice.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(invoice => invoice.RepairRequest.WarrantyCard.CustomerId == customerId.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<bool> ExistsForRepairRequestAsync(int repairRequestId, CancellationToken cancellationToken = default) =>
        dbContext.Invoices.AsNoTracking().AnyAsync(
            invoice => invoice.RepairRequestId == repairRequestId && invoice.Status != InvoiceStatus.Cancelled,
            cancellationToken);

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
        await dbContext.Invoices.AddAsync(invoice, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task SaveChangesWithNotificationAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static IQueryable<Invoice> WithDetails(IQueryable<Invoice> query) =>
        query.Include(invoice => invoice.IssuedBy)
            .Include(invoice => invoice.Payments)
            .Include(invoice => invoice.RepairRequest)
                .ThenInclude(request => request.WarrantyCard)
            .Include(invoice => invoice.RepairRequest)
                .ThenInclude(request => request.Parts)
                    .ThenInclude(item => item.Part);
}
