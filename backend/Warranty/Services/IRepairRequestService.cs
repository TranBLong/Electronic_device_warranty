using Warranty.Dtos;

namespace Warranty.Services;

public interface IRepairRequestService
{
    Task<ServiceResult<RepairRequestResponse>> CreateAsync(
        CreateRepairRequest request,
        int actorId,
        bool isCustomer,
        bool isReceptionist,
        CancellationToken cancellationToken);

    Task<ServiceResult<RepairRequestResponse>> AssignTechnicianAsync(
        int repairRequestId,
        int technicianId,
        CancellationToken cancellationToken);

    Task<ServiceResult<RepairRequestResponse>> ChangeStatusAsync(
        int repairRequestId,
        ChangeRepairStatusRequest request,
        int actorId,
        bool isTechnician,
        bool isManagerOrAdmin,
        CancellationToken cancellationToken);

    Task<ServiceResult<RepairPartLineResponse>> AddPartAsync(
        int repairRequestId,
        AddRepairPartRequest request,
        int actorId,
        bool isTechnician,
        bool isManagerOrAdmin,
        CancellationToken cancellationToken);
}
