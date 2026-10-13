using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Worth;

public interface IRepairRequestPartRepository
{
    Task<IReadOnlyList<RepairRequestPart>> GetByRepairRequestIdAsync(
        int repairRequestId,
        CancellationToken cancellationToken = default);
    Task<RepairRequestPart?> AddWithStockDecrementAsync(
        int repairRequestId,
        int partId,
        int quantity,
        CancellationToken cancellationToken = default);
}
