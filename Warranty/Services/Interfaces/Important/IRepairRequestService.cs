using Warranty.Dtos;
using Warranty.Enums;

namespace Warranty.Services.Interfaces.Important;

public interface IRepairRequestService
{
    Task<PagedResult<RepairRequestResponse>> GetAllAsync(
        int userId,
        UserRole role,
        RepairRequestStatus? status,
        int? technicianId,
        DateTime? createdFrom,
        DateTime? createdTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<RepairRequestResponse> GetByIdAsync(
        int id,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default);
    Task<RepairRequestResponse> CreateAsync(
        CreateRepairRequest request,
        int actorId,
        UserRole role,
        CancellationToken cancellationToken = default);
    Task<RepairRequestResponse> AssignTechnicianAsync(
        int id,
        int technicianId,
        UserRole role,
        CancellationToken cancellationToken = default);
    Task<RepairRequestResponse> AssignServiceCenterAsync(
        int id,
        int serviceCenterId,
        UserRole role,
        CancellationToken cancellationToken = default);
    Task<RepairRequestResponse> ChangeStatusAsync(
        int id,
        RepairRequestStatus status,
        int actorId,
        UserRole role,
        CancellationToken cancellationToken = default);
    Task<RepairRequestResponse> SetChargeabilityAsync(
        int id,
        RepairRequestChargeabilityDto request,
        int actorId,
        UserRole role,
        CancellationToken cancellationToken = default);
    Task<RepairRequestResponse> UpdateDetailsAsync(
        int id,
        UpdateRepairRequestDetailsDto request,
        int actorId,
        UserRole role,
        CancellationToken cancellationToken = default);
}
