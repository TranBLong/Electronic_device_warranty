using Warranty.Enums;
using System.ComponentModel.DataAnnotations;

namespace Warranty.Dtos;

public sealed record InvoiceCreateDto;

public sealed record InvoiceCancelDto([StringLength(500)] string? CancellationReason = null);

public sealed record InvoiceLineResponseDto(
    int RepairRequestPartId,
    int PartId,
    string PartName,
    string PartNumber,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record InvoiceResponseDto(
    int Id,
    string InvoiceNumber,
    int RepairRequestId,
    InvoiceStatus Status,
    decimal PartsTotal,
    decimal LaborTotal,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    DateTime IssuedAt,
    int IssuedById,
    string IssuedByName,
    DateTime? PaidAt,
    DateTime? CancelledAt,
    int? CancelledById,
    string? CancellationReason,
    IReadOnlyList<InvoiceLineResponseDto> Lines);

public sealed record InvoiceListResponseDto(
    int Id,
    string InvoiceNumber,
    int RepairRequestId,
    InvoiceStatus Status,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    DateTime IssuedAt);
