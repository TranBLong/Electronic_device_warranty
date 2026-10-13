using Warranty.Dtos;

namespace Warranty.Services.Interfaces.Worth;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponseDto>> GetAllAsync(
        string? name,
        CancellationToken cancellationToken = default);
    Task<CategoryResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryResponseDto> CreateAsync(CategoryCreateDto request, CancellationToken cancellationToken = default);
    Task<CategoryResponseDto> UpdateAsync(
        int id,
        CategoryUpdateDto request,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
