using Warranty.Dtos;
using Warranty.Enums;

namespace Warranty.Services.Interfaces.Complete;

public interface IFeedbackService
{
    Task<FeedbackResponseDto> CreateAsync(int repairRequestId, FeedbackCreateDto request, int customerId, UserRole role, CancellationToken cancellationToken = default);
    Task<FeedbackResponseDto> GetByRepairRequestIdAsync(int repairRequestId, int userId, UserRole role, CancellationToken cancellationToken = default);
    Task<PagedResponseDto<FeedbackResponseDto>> GetAllAsync(int? rating, int page, int pageSize, CancellationToken cancellationToken = default);
}
