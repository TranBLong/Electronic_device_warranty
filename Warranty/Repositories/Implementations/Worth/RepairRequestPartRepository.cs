using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Models;
using Warranty.Dtos;
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

    public async Task<bool> RemoveWithStockIncrementAsync(
        int repairRequestId,
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var usage = await dbContext.RepairRequestParts
            .SingleOrDefaultAsync(
                item => item.Id == id && item.RepairRequestId == repairRequestId,
                cancellationToken);
        if (usage is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var rowsUpdated = await dbContext.Parts
            .Where(part => part.Id == usage.PartId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    part => part.StockQuantity,
                    part => part.StockQuantity + usage.Quantity),
                cancellationToken);
        if (rowsUpdated == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.RepairRequestParts.Remove(usage);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<TopPartItem>> GetTopPartsAsync(
        int top, CancellationToken cancellationToken = default) =>
        await dbContext.RepairRequestParts.AsNoTracking()
            .GroupBy(item => new { item.PartId, item.Part.PartNumber, item.Part.Name })
            .Select(group => new TopPartItem(group.Key.PartId, group.Key.PartNumber, group.Key.Name,
                group.Sum(item => item.Quantity), group.Sum(item => item.Quantity * item.UnitPrice)))
            .OrderByDescending(item => item.TotalQuantity).ThenByDescending(item => item.TotalAmount)
            .Take(top).ToListAsync(cancellationToken);

    private static IQueryable<RepairRequestPart> WithDetails(IQueryable<RepairRequestPart> query) =>
        query.Include(item => item.Part);
}
