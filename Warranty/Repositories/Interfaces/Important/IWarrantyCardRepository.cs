using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Important;

public interface IWarrantyCardRepository
{
    Task<IReadOnlyList<WarrantyCard>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarrantyCard>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarrantyCard>> SearchAsync(int? customerId, WarrantyStatus? status, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountSearchAsync(int? customerId, WarrantyStatus? status, string? keyword, CancellationToken cancellationToken = default);
    Task<WarrantyCard?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WarrantyCard?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> HasBlockingRepairRequestsAsync(int warrantyCardId, CancellationToken cancellationToken = default);
    Task VoidAsync(WarrantyCard card, Notification notification, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountActiveUnexpiredAsync(DateTime today, CancellationToken cancellationToken = default);
    Task AddAsync(WarrantyCard warrantyCard, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
