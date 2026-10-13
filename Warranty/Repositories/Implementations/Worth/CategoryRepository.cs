using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Worth;

namespace Warranty.Repositories.Implementations.Worth;

public sealed class CategoryRepository(AppDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(
        string? name,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(category => category.Name.Contains(name));
        }

        return await query.OrderBy(category => category.Name).ToListAsync(cancellationToken);
    }

    public Task<Category?> GetByIdAsync(
        int id,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        int? exceptId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Categories.AsNoTracking()
            .AnyAsync(category => category.Name == name && category.Id != exceptId, cancellationToken);

    public Task<bool> HasPartsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Parts.AsNoTracking().AnyAsync(part => part.CategoryId == id, cancellationToken);

    public Task<bool> HasProductsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Products.AsNoTracking().AnyAsync(product => product.CategoryId == id, cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default) =>
        await dbContext.Categories.AddAsync(category, cancellationToken);

    public void Remove(Category category) => dbContext.Categories.Remove(category);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
