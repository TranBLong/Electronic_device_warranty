using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;

namespace Warranty.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/reports")]
public sealed class ReportsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var result = new DashboardSummaryResponse(
            await dbContext.WarrantyCards.CountAsync(cancellationToken),
            await dbContext.WarrantyCards.CountAsync(
                card => card.Status == WarrantyStatus.Active && card.EndDate >= today,
                cancellationToken),
            await dbContext.RepairRequests.CountAsync(cancellationToken),
            await dbContext.RepairRequests.CountAsync(
                request => request.Status == RepairRequestStatus.Received,
                cancellationToken),
            await dbContext.RepairRequests.CountAsync(
                request => request.Status == RepairRequestStatus.InProgress,
                cancellationToken),
            await dbContext.RepairRequests.CountAsync(
                request => request.Status == RepairRequestStatus.Completed,
                cancellationToken));
        return Ok(result);
    }
}
