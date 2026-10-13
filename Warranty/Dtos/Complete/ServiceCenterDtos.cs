using System.ComponentModel.DataAnnotations;

namespace Warranty.Dtos;

public sealed record ServiceCenterCreateDto(
    [Required, StringLength(200)] string Name,
    [Required, StringLength(500)] string Address,
    [StringLength(30)] string? Phone,
    [Range(1, int.MaxValue)] int ManagerId);

public sealed record ServiceCenterUpdateDto(
    [Required, StringLength(200)] string Name,
    [Required, StringLength(500)] string Address,
    [StringLength(30)] string? Phone,
    [Range(1, int.MaxValue)] int ManagerId);

public sealed record ServiceCenterResponseDto(
    int Id,
    string Name,
    string Address,
    string? Phone,
    int ManagerId,
    string ManagerName,
    bool IsActive,
    DateTime CreatedAt);

public sealed record RepairRequestServiceCenterDto([Range(1, int.MaxValue)] int ServiceCenterId);

public sealed record ServiceCenterActiveRequest(bool IsActive);
