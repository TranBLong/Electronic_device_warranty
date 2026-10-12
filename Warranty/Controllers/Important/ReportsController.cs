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
}
