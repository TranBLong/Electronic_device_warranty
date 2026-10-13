using Warranty.Dtos;

namespace Warranty.Services.Interfaces.Worth;

public interface IPartService
{
    Task<PartListResponseDto> GetAllAsync(
        int? categoryId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PartResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PartResponseDto> CreateAsync(PartCreateDto request, CancellationToken cancellationToken = default);
    Task<PartResponseDto> UpdateAsync(int id, PartUpdateDto request, CancellationToken cancellationToken = default);
    Task<PartResponseDto> UpdateStockAsync(
        int id,
        PartStockUpdateDto request,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
