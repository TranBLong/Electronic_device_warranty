using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty.Dtos;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/reports")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await reportService.GetSummaryAsync(cancellationToken));

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportResponse>> GetRevenue(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken) =>
        Ok(await reportService.GetRevenueAsync(from, to, cancellationToken));

    [HttpGet("technicians")]
    public async Task<ActionResult<TechnicianPerformanceResponse>> GetTechnicians(CancellationToken cancellationToken) =>
        Ok(await reportService.GetTechnicianPerformanceAsync(cancellationToken));

    [HttpGet("top-parts")]
    public async Task<ActionResult<TopPartsReportResponse>> GetTopParts(
        [FromQuery] int top = 10, CancellationToken cancellationToken = default) =>
        Ok(await reportService.GetTopPartsAsync(top, cancellationToken));

    [HttpGet("low-stock")]
    public async Task<ActionResult<LowStockReportResponse>> GetLowStock(
        [FromQuery] int threshold = 5, CancellationToken cancellationToken = default) =>
        Ok(await reportService.GetLowStockAsync(threshold, cancellationToken));

    [HttpGet("feedback")]
    public async Task<ActionResult<FeedbackReportResponse>> GetFeedback(CancellationToken cancellationToken) =>
        Ok(await reportService.GetFeedbackSummaryAsync(cancellationToken));
}
