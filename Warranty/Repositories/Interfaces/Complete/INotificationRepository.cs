using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Complete;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountForUserAsync(int userId, bool unreadOnly, CancellationToken cancellationToken = default);
    Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
