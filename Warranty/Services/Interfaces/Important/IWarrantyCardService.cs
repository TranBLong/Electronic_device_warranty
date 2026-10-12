using Warranty.Dtos;
using Warranty.Enums;

namespace Warranty.Services.Interfaces.Important;

public interface IWarrantyCardService
{
    Task<IReadOnlyList<WarrantyCardResponse>> GetAllAsync(
        int userId,
        UserRole role,
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
