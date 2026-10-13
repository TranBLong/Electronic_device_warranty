using Warranty.Dtos;

namespace Warranty.Services.Interfaces.Complete;

public interface IServiceCenterService
{
    Task<IReadOnlyList<ServiceCenterResponseDto>> GetAllAsync(string? name, bool? isActive, CancellationToken cancellationToken = default);
    Task<ServiceCenterResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceCenterResponseDto> CreateAsync(ServiceCenterCreateDto request, CancellationToken cancellationToken = default);
    Task<ServiceCenterResponseDto> UpdateAsync(int id, ServiceCenterUpdateDto request, CancellationToken cancellationToken = default);
    Task<ServiceCenterResponseDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
