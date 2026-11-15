using Warranty.Enums;
using Warranty.Models;
using Warranty.Dtos;

namespace Warranty.Repositories.Interfaces.Important;

public interface IRepairRequestRepository
{
    Task<IReadOnlyList<RepairRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepairRequest>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepairRequest>> GetByTechnicianIdAsync(
        int technicianId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepairRequest>> SearchAsync(int? customerId, int? assignedTechnicianId, RepairRequestStatus? status, int? filterTechnicianId, DateTime? createdFrom, DateTime? createdTo, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountSearchAsync(int? customerId, int? assignedTechnicianId, RepairRequestStatus? status, int? filterTechnicianId, DateTime? createdFrom, DateTime? createdTo, CancellationToken cancellationToken = default);
    Task<RepairRequest?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(RepairRequestStatus status, CancellationToken cancellationToken = default);
    Task AddAsync(RepairRequest repairRequest, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task SaveStatusChangeAsync(
        RepairRequest repairRequest,
        Notification notification,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TechnicianPerformanceItem>> GetTechnicianPerformanceAsync(
        CancellationToken cancellationToken = default);
}
