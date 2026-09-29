using System.ComponentModel.DataAnnotations;
using Warranty.Enums;

namespace Warranty.Dtos;

public sealed class CreateWarrantyCardRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int CustomerId { get; init; }

    public DateTime StartDate { get; init; }
}

public sealed record WarrantyCardResponse(
    int Id,
    int ProductId,
    string ProductName,
    string SerialNumber,
    int CustomerId,
    string CustomerName,
    DateTime StartDate,
    DateTime EndDate,
    WarrantyStatus Status);
