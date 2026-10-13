namespace Warranty.Dtos;

public sealed record RepairRequestPartCreateDto(int PartId, int Quantity);

public sealed record RepairRequestPartResponseDto(
    int Id,
    int RepairRequestId,
    int PartId,
    string PartName,
    string PartNumber,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    DateTime UsedAt);
