using Warranty.Dtos;

namespace Warranty.Services.Interfaces.Important;

public interface IReportService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
}
