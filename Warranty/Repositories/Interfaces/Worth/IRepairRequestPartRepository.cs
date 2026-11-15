using Warranty.Models;
using Warranty.Dtos;

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
    Task<bool> RemoveWithStockIncrementAsync(
        int repairRequestId,
        int id,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopPartItem>> GetTopPartsAsync(int top, CancellationToken cancellationToken = default);
}
