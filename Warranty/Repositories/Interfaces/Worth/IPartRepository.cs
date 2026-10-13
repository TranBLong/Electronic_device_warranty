using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Worth;

public interface IPartRepository
{
    Task<(IReadOnlyList<Part> Items, int TotalCount)> GetAllAsync(
        int? categoryId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Part?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPartNumberAsync(
        string partNumber,
        int? exceptId = null,
        CancellationToken cancellationToken = default);
    Task<bool> HasRepairRequestPartsAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Part part, CancellationToken cancellationToken = default);
    void Remove(Part part);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
