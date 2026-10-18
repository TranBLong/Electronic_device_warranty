using System.ComponentModel.DataAnnotations;

namespace Warranty.Dtos;

public sealed class ProductRequest
{
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Brand { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Model { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string SerialNumber { get; init; } = string.Empty;

    [Range(1, 600)]
    public int WarrantyMonths { get; init; }
}

public sealed record ProductResponse(
    int Id,
    int? CategoryId,
    string Name,
    string Brand,
    string Model,
    string SerialNumber,
    int WarrantyMonths,
    DateTime CreatedAt);
