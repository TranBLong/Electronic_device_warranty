using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Important;

namespace Warranty.Repositories.Implementations.Important;

public sealed class ProductRepository(AppDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Products.AsNoTracking().OrderBy(product => product.Name).ToListAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(
        int id,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public Task<bool> SerialNumberExistsAsync(
        string serialNumber,
        int? exceptProductId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Products.AsNoTracking().AnyAsync(
            product => product.SerialNumber == serialNumber &&
                       (!exceptProductId.HasValue || product.Id != exceptProductId.Value),
            cancellationToken);

    public Task<bool> HasWarrantyCardsAsync(int productId, CancellationToken cancellationToken = default) =>
        dbContext.WarrantyCards.AsNoTracking().AnyAsync(card => card.ProductId == productId, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        dbContext.Products.AsNoTracking().CountAsync(cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await dbContext.Products.AddAsync(product, cancellationToken);

    public void Delete(Product product) => dbContext.Products.Remove(product);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
