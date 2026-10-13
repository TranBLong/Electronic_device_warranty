using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Worth;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(string? name, CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? exceptId = null, CancellationToken cancellationToken = default);
    Task<bool> HasPartsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasProductsAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Remove(Category category);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
