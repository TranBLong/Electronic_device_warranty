using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;

namespace Warranty.Repositories.Implementations.Complete;

public sealed class FeedbackRepository(AppDbContext dbContext) : IFeedbackRepository
{
    public Task<Feedback?> GetByRepairRequestIdAsync(
        int repairRequestId,
        CancellationToken cancellationToken = default) =>
        dbContext.Feedbacks.AsNoTracking()
            .Include(feedback => feedback.Customer)
            .SingleOrDefaultAsync(feedback => feedback.RepairRequestId == repairRequestId, cancellationToken);

    public async Task<IReadOnlyList<Feedback>> GetAllAsync(
        int? rating,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Feedbacks.AsNoTracking().Include(feedback => feedback.Customer).AsQueryable();
        if (rating.HasValue)
        {
            query = query.Where(feedback => feedback.Rating == rating.Value);
        }

        return await query.OrderByDescending(feedback => feedback.CreatedAt)
            .ThenByDescending(feedback => feedback.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(int? rating, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Feedbacks.AsNoTracking().AsQueryable();
        if (rating.HasValue)
        {
            query = query.Where(feedback => feedback.Rating == rating.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Feedback feedback, CancellationToken cancellationToken = default) =>
        await dbContext.Feedbacks.AddAsync(feedback, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
