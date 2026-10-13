using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Worth;

namespace Warranty.Repositories.Implementations.Worth;

public sealed class RepairRequestPartRepository(AppDbContext dbContext) : IRepairRequestPartRepository
{
    public async Task<IReadOnlyList<RepairRequestPart>> GetByRepairRequestIdAsync(
        int repairRequestId,
        CancellationToken cancellationToken = default) =>
        await WithDetails(dbContext.RepairRequestParts.AsNoTracking())
            .Where(item => item.RepairRequestId == repairRequestId)
            .OrderBy(item => item.UsedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task<RepairRequestPart?> AddWithStockDecrementAsync(
        int repairRequestId,
        int partId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var rowsUpdated = await dbContext.Parts
            .Where(part => part.Id == partId && part.StockQuantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(part => part.StockQuantity, part => part.StockQuantity - quantity),
                cancellationToken);

        if (rowsUpdated == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var part = await dbContext.Parts.AsNoTracking()
            .SingleAsync(item => item.Id == partId, cancellationToken);
        var usage = new RepairRequestPart
        {
            RepairRequestId = repairRequestId,
            PartId = partId,
            Quantity = quantity,
            UnitPrice = part.UnitPrice,
            UsedAt = DateTime.UtcNow
        };
        await dbContext.RepairRequestParts.AddAsync(usage, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await WithDetails(dbContext.RepairRequestParts.AsNoTracking())
            .SingleAsync(item => item.Id == usage.Id, cancellationToken);
    }

    private static IQueryable<RepairRequestPart> WithDetails(IQueryable<RepairRequestPart> query) =>
        query.Include(item => item.Part);
}
