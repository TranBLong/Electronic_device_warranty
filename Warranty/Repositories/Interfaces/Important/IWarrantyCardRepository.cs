using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Important;

public interface IWarrantyCardRepository
{
    Task<IReadOnlyList<WarrantyCard>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarrantyCard>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default);
    Task<WarrantyCard?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountActiveUnexpiredAsync(DateTime today, CancellationToken cancellationToken = default);
    Task AddAsync(WarrantyCard warrantyCard, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
