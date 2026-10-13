using Warranty.Dtos;
using Warranty.Exceptions;
using Warranty.Repositories.Interfaces.Complete;
using Warranty.Services.Interfaces.Complete;

namespace Warranty.Services.Implementations.Complete;

public sealed class NotificationService(INotificationRepository notificationRepository) : INotificationService
{
    public async Task<PagedResponseDto<NotificationResponseDto>> GetMineAsync(
        int userId,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);
        var items = await notificationRepository.GetForUserAsync(userId, unreadOnly, page, pageSize, cancellationToken);
        var total = await notificationRepository.CountForUserAsync(userId, unreadOnly, cancellationToken);
        return new PagedResponseDto<NotificationResponseDto>(
            items.Select(item => new NotificationResponseDto(
                item.Id, item.RepairRequestId, item.Type, item.Title, item.Message, item.IsRead, item.CreatedAt)).ToList(),
            page, pageSize, total);
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default) =>
        notificationRepository.CountUnreadAsync(userId, cancellationToken);

    public async Task MarkReadAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var notification = await notificationRepository.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException("Notification was not found.");
        if (notification.UserId != userId)
        {
            throw new ForbiddenException("You are not allowed to update this notification.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await notificationRepository.SaveChangesAsync(cancellationToken);
        }
    }

    public Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default) =>
        notificationRepository.MarkAllReadAsync(userId, cancellationToken);

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
        {
            throw new BusinessRuleException("Page must be positive and pageSize must be between 1 and 100.");
        }
    }
}
