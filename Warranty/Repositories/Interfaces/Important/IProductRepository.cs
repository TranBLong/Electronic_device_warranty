using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Important;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default);
    Task<bool> SerialNumberExistsAsync(
        string serialNumber,
        int? exceptProductId = null,
        CancellationToken cancellationToken = default);
    Task<bool> HasWarrantyCardsAsync(int productId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Delete(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
