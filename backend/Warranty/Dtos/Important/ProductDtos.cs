using System.ComponentModel.DataAnnotations;

namespace Warranty.Dtos;

public sealed class ProductRequest
{
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
    string Name,
    string Brand,
    string Model,
    string SerialNumber,
    int WarrantyMonths,
    DateTime CreatedAt);
