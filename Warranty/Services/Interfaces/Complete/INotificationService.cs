using Warranty.Dtos;

namespace Warranty.Services.Interfaces.Complete;

public interface INotificationService
{
    Task<PagedResponseDto<NotificationResponseDto>> GetMineAsync(int userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task MarkReadAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
}
