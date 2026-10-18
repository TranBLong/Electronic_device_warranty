using Warranty.Dtos;
using Warranty.Enums;

namespace Warranty.Services.Interfaces.Complete;

public interface IInvoiceService
{
    Task<InvoiceResponseDto> CreateAsync(int repairRequestId, int userId, UserRole role, CancellationToken cancellationToken = default);
    Task<InvoiceResponseDto> GetByIdAsync(int id, int userId, UserRole role, CancellationToken cancellationToken = default);
    Task<InvoiceResponseDto> GetByRepairRequestIdAsync(int repairRequestId, int userId, UserRole role, CancellationToken cancellationToken = default);
    Task<PagedResponseDto<InvoiceListResponseDto>> GetAllAsync(InvoiceStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponseDto<InvoiceListResponseDto>> GetMineAsync(int customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task CancelAsync(
        int id,
        int actorUserId,
        UserRole role,
        string? cancellationReason,
        CancellationToken cancellationToken = default);
}
