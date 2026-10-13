using Warranty.Enums;

namespace Warranty.Dtos;

public sealed record NotificationResponseDto(
    int Id,
    int? RepairRequestId,
    NotificationType Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt);

public sealed record PagedResponseDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
