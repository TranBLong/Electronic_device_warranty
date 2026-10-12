using Warranty.Dtos;

namespace Warranty.Services.Interfaces.Important;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateAsync(int id, ProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
