using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Enums;

namespace Warranty.Services;

public sealed class WarrantyExpiryBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<WarrantyExpiryBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WarrantyExpiryBackgroundService started");
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var today = DateTime.UtcNow.Date;
                var expired = await db.WarrantyCards
                    .Where(c => c.Status == WarrantyStatus.Active && c.EndDate < today)
                    .ToListAsync(stoppingToken);
                if (expired.Count > 0)
                {
                    foreach (var c in expired) c.Status = WarrantyStatus.Expired;
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Expired {Count} warranty cards", expired.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Warranty expiry job failed"); }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
