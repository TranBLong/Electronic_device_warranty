using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Worth;

namespace Warranty.Repositories.Implementations.Worth;

public sealed class PartRepository(AppDbContext dbContext) : IPartRepository
{
    public async Task<(IReadOnlyList<Part> Items, int TotalCount)> GetAllAsync(
        int? categoryId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = WithCategory(dbContext.Parts.AsNoTracking());
        if (categoryId.HasValue)
        {
            query = query.Where(part => part.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(part => part.Name.Contains(search) || part.PartNumber.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(part => part.Name)
            .ThenBy(part => part.PartNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<Part?> GetByIdAsync(
        int id,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Parts.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return WithCategory(query).SingleOrDefaultAsync(part => part.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByPartNumberAsync(
        string partNumber,
        int? exceptId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Parts.AsNoTracking()
            .AnyAsync(part => part.PartNumber == partNumber && part.Id != exceptId, cancellationToken);

    public Task<bool> HasRepairRequestPartsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.RepairRequestParts.AsNoTracking().AnyAsync(item => item.PartId == id, cancellationToken);

    public async Task AddAsync(Part part, CancellationToken cancellationToken = default) =>
        await dbContext.Parts.AddAsync(part, cancellationToken);

    public void Remove(Part part) => dbContext.Parts.Remove(part);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Part> WithCategory(IQueryable<Part> query) =>
        query.Include(part => part.Category);
}
