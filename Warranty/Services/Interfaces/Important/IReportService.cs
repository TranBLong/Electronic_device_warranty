using Warranty.Dtos;

namespace Warranty.Services.Interfaces.Important;

public interface IReportService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<RevenueReportResponse> GetRevenueAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<TechnicianPerformanceResponse> GetTechnicianPerformanceAsync(CancellationToken cancellationToken = default);
    Task<TopPartsReportResponse> GetTopPartsAsync(int top, CancellationToken cancellationToken = default);
    Task<LowStockReportResponse> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default);
    Task<FeedbackReportResponse> GetFeedbackSummaryAsync(CancellationToken cancellationToken = default);
}
