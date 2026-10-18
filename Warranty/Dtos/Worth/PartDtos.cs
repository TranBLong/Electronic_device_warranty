namespace Warranty.Dtos;

public sealed record PartCreateDto(
    int CategoryId,
    string Name,
    string PartNumber,
    string? Description,
    decimal UnitPrice,
    int StockQuantity);

public sealed record PartUpdateDto(
    int CategoryId,
    string Name,
    string PartNumber,
    string? Description,
    decimal UnitPrice,
    int StockQuantity,
    bool? IsActive = null);

public sealed record PartStockUpdateDto(int StockQuantity);

public sealed record PartResponseDto(
    int Id,
    int CategoryId,
    string CategoryName,
    string Name,
    string PartNumber,
    string? Description,
    decimal UnitPrice,
    int StockQuantity,
    bool IsActive,
    DateTime CreatedAt);

public sealed record PartListResponseDto(
    IReadOnlyList<PartResponseDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
