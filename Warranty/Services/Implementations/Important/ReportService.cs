using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Repositories.Interfaces.Complete;
using Warranty.Repositories.Interfaces.Worth;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Services.Implementations.Important;

public sealed class ReportService(
    IWarrantyCardRepository warrantyCardRepository,
    IRepairRequestRepository repairRequestRepository,
    IPaymentRepository paymentRepository,
    IRepairRequestPartRepository repairRequestPartRepository,
    IPartRepository partRepository,
    IFeedbackRepository feedbackRepository) : IReportService
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

    public async Task<RevenueReportResponse> GetRevenueAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        ValidateDateRange(from, to);
        var fromUtc = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to.Date, DateTimeKind.Utc);
        var rows = await paymentRepository.GetMonthlyRevenueAsync(fromUtc, toUtc, cancellationToken);
        var items = rows.Select(row => new MonthlyRevenueItem(row.Year, row.Month, row.Total)).ToList();
        return new RevenueReportResponse(items, items.Sum(item => item.TotalAmount));
    }

    public async Task<TechnicianPerformanceResponse> GetTechnicianPerformanceAsync(
        CancellationToken cancellationToken = default) =>
        new(await repairRequestRepository.GetTechnicianPerformanceAsync(cancellationToken));

    public async Task<TopPartsReportResponse> GetTopPartsAsync(
        int top, CancellationToken cancellationToken = default)
    {
        if (top is < 1 or > 100) throw new BusinessRuleException("top must be between 1 and 100.");
        return new TopPartsReportResponse(await repairRequestPartRepository.GetTopPartsAsync(top, cancellationToken));
    }

    public async Task<LowStockReportResponse> GetLowStockAsync(
        int threshold, CancellationToken cancellationToken = default)
    {
        if (threshold < 0) throw new BusinessRuleException("threshold must be non-negative.");
        return new LowStockReportResponse(await partRepository.GetLowStockAsync(threshold, cancellationToken));
    }

    public Task<FeedbackReportResponse> GetFeedbackSummaryAsync(CancellationToken cancellationToken = default) =>
        feedbackRepository.GetRatingSummaryAsync(cancellationToken);

    private static void ValidateDateRange(DateTime from, DateTime to)
    {
        if (from.Date > to.Date) throw new BusinessRuleException("from must be less than or equal to to.");
        if ((to.Date - from.Date).TotalDays > 366)
            throw new BusinessRuleException("Date range must not exceed 366 days.");
    }
}
