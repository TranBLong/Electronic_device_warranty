using Warranty.Dtos;
using Warranty.Enums;

namespace Warranty.Services.Interfaces.Worth;

public interface IRepairRequestPartService
{
    Task<IReadOnlyList<RepairRequestPartResponseDto>> GetByRepairRequestIdAsync(
        int repairRequestId,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default);
    Task<RepairRequestPartResponseDto> AddAsync(
        int repairRequestId,
        RepairRequestPartCreateDto request,
        int technicianId,
        UserRole role,
        CancellationToken cancellationToken = default);
}
