using Microsoft.EntityFrameworkCore;
using Warranty.Data;
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

    public Task<WarrantyCard?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        WithDetails(dbContext.WarrantyCards.AsNoTracking())
            .SingleOrDefaultAsync(card => card.Id == id, cancellationToken);

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
}
