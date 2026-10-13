using System.ComponentModel.DataAnnotations;

namespace Warranty.Dtos;

public sealed record FeedbackCreateDto(
    [Range(1, 5)] int Rating,
    [StringLength(1000)] string? Comment);

public sealed record FeedbackResponseDto(
    int Id,
    int RepairRequestId,
    int CustomerId,
    string CustomerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt);
