using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Services.Implementations.Important;

public sealed class ReportService(
    IWarrantyCardRepository warrantyCardRepository,
    IRepairRequestRepository repairRequestRepository) : IReportService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return new DashboardSummaryResponse(
            await warrantyCardRepository.CountAsync(cancellationToken),
            await warrantyCardRepository.CountActiveUnexpiredAsync(today, cancellationToken),
            await repairRequestRepository.CountAsync(cancellationToken),
            await repairRequestRepository.CountByStatusAsync(RepairRequestStatus.Received, cancellationToken),
            await repairRequestRepository.CountByStatusAsync(RepairRequestStatus.InProgress, cancellationToken),
            await repairRequestRepository.CountByStatusAsync(RepairRequestStatus.Completed, cancellationToken));
    }
}
