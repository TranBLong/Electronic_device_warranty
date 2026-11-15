using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Important;

namespace Warranty.Repositories.Implementations.Important;

public sealed class WarrantyCardRepository(AppDbContext dbContext) : IWarrantyCardRepository
{
    public async Task<IReadOnlyList<WarrantyCard>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await WithDetails(dbContext.WarrantyCards.AsNoTracking())
            .OrderByDescending(card => card.StartDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WarrantyCard>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default) =>
        await WithDetails(dbContext.WarrantyCards.AsNoTracking())
            .Where(card => card.CustomerId == customerId)
            .OrderByDescending(card => card.StartDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WarrantyCard>> SearchAsync(
        int? customerId, WarrantyStatus? status, string? keyword, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySearchFilter(WithDetails(dbContext.WarrantyCards.AsNoTracking()), customerId, status, keyword);
        return await query.OrderByDescending(card => card.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    }

    public Task<int> CountSearchAsync(
        int? customerId, WarrantyStatus? status, string? keyword,
        CancellationToken cancellationToken = default) =>
        ApplySearchFilter(dbContext.WarrantyCards.AsNoTracking(), customerId, status, keyword)
            .CountAsync(cancellationToken);

    public Task<WarrantyCard?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, tracking: false, cancellationToken);

    public Task<WarrantyCard?> GetByIdAsync(
        int id, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = dbContext.WarrantyCards.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return WithDetails(query).SingleOrDefaultAsync(card => card.Id == id, cancellationToken);
    }

    public Task<bool> HasBlockingRepairRequestsAsync(
        int warrantyCardId, CancellationToken cancellationToken = default) =>
        dbContext.RepairRequests.AsNoTracking().AnyAsync(request =>
            request.WarrantyCardId == warrantyCardId &&
            (request.Status == RepairRequestStatus.Received ||
             request.Status == RepairRequestStatus.InProgress ||
             request.Status == RepairRequestStatus.Completed), cancellationToken);

    public async Task VoidAsync(
        WarrantyCard card, Notification notification, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        dbContext.WarrantyCards.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountActiveUnexpiredAsync(DateTime today, CancellationToken cancellationToken = default) =>
        dbContext.WarrantyCards.AsNoTracking()
            .CountAsync(card => card.Status == Enums.WarrantyStatus.Active && card.EndDate >= today, cancellationToken);

    public async Task AddAsync(WarrantyCard warrantyCard, CancellationToken cancellationToken = default) =>
        await dbContext.WarrantyCards.AddAsync(warrantyCard, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<WarrantyCard> WithDetails(IQueryable<WarrantyCard> query) =>
        query.Include(card => card.Product).Include(card => card.Customer);

    private static IQueryable<WarrantyCard> ApplySearchFilter(
        IQueryable<WarrantyCard> query, int? customerId, WarrantyStatus? status, string? keyword)
    {
        if (customerId.HasValue) query = query.Where(card => card.CustomerId == customerId.Value);
        if (status.HasValue) query = query.Where(card => card.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(card => card.Customer.FullName.Contains(term) || card.Product.SerialNumber.Contains(term));
        }
        return query;
    }
}
