using Warranty.Dtos;
using Warranty.Enums;

namespace Warranty.Services.Interfaces.Complete;

public interface IPaymentService
{
    Task<PaymentResponseDto> CreateAsync(int invoiceId, PaymentCreateDto request, int userId, UserRole role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentResponseDto>> GetByInvoiceIdAsync(int invoiceId, int userId, UserRole role, CancellationToken cancellationToken = default);
}
