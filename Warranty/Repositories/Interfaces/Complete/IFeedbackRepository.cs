using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Complete;

public interface IFeedbackRepository
{
    Task<Feedback?> GetByRepairRequestIdAsync(int repairRequestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Feedback>> GetAllAsync(int? rating, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(int? rating, CancellationToken cancellationToken = default);
    Task AddAsync(Feedback feedback, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
