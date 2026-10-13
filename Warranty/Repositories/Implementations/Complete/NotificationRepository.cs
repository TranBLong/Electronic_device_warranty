using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;

namespace Warranty.Repositories.Implementations.Complete;

public sealed class NotificationRepository(AppDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetForUserAsync(
        int userId,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications.AsNoTracking().Where(notification => notification.UserId == userId);
        if (unreadOnly)
        {
            query = query.Where(notification => !notification.IsRead);
        }

        return await query.OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountForUserAsync(
        int userId,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications.AsNoTracking().Where(notification => notification.UserId == userId);
        if (unreadOnly)
        {
            query = query.Where(notification => !notification.IsRead);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.AsNoTracking()
            .CountAsync(notification => notification.UserId == userId && !notification.IsRead, cancellationToken);

    public Task<Notification?> GetByIdAsync(
        int id,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(notification => notification.Id == id, cancellationToken);
    }

    public async Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.IsRead, true), cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
