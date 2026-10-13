using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Complete;

public interface IRepairRequestAttachmentRepository
{
    Task<IReadOnlyList<RepairRequestAttachment>> GetByRepairRequestIdAsync(int repairRequestId, CancellationToken cancellationToken = default);
    Task<RepairRequestAttachment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(RepairRequestAttachment attachment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
