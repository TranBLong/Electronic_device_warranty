namespace Warranty.Dtos;

public sealed record CategoryCreateDto(string Name, string? Description);

public sealed record CategoryUpdateDto(string Name, string? Description);

public sealed record CategoryResponseDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);
