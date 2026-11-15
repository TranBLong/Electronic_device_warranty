using Warranty.Dtos;
using Warranty.Enums;

namespace Warranty.Services.Interfaces.Important;

public interface IWarrantyCardService
{
    Task<PagedResult<WarrantyCardResponse>> GetAllAsync(
        int userId,
        UserRole role,
        WarrantyStatus? status,
        string? keyword,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<WarrantyCardResponse> GetByIdAsync(
        int id,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default);
    Task<WarrantyCardResponse> CreateAsync(
        CreateWarrantyCardRequest request,
        UserRole role,
        CancellationToken cancellationToken = default);
}
