using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Complete;

public interface IServiceCenterRepository
{
    Task<IReadOnlyList<ServiceCenter>> GetAllAsync(string? name, bool? isActive, CancellationToken cancellationToken = default);
    Task<ServiceCenter?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? exceptId = null, CancellationToken cancellationToken = default);
    Task<bool> HasRepairRequestsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasStaffAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(ServiceCenter center, CancellationToken cancellationToken = default);
    void Remove(ServiceCenter center);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
