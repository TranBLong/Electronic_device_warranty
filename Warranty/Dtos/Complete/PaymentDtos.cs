using System.ComponentModel.DataAnnotations;
using Warranty.Enums;

namespace Warranty.Dtos;

public sealed record PaymentCreateDto(
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal Amount,
    [EnumDataType(typeof(PaymentMethod))] PaymentMethod Method,
    [StringLength(500)] string? Note);

public sealed record PaymentResponseDto(
    int Id,
    int InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    string? Note,
    DateTime PaidAt,
    int ReceivedById,
    string ReceivedByName);
