using EWarrantySystem.Data;
using Microsoft.EntityFrameworkCore;

namespace EWarrantySystem.Services;

public class WarrantyExpiryAlertService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<WarrantyExpiryAlertService> _logger;

    public WarrantyExpiryAlertService(IServiceProvider sp, ILogger<WarrantyExpiryAlertService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndNotifyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra bảo hành sắp hết hạn");
            }

            var hours = 24;
            using (var scope = _sp.CreateScope())
            {
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                hours = int.Parse(config["WarrantyAlert:RunIntervalHours"] ?? "24");
            }
            await Task.Delay(TimeSpan.FromHours(hours), stoppingToken);
        }
    }

    private async Task CheckAndNotifyAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var days = int.Parse(config["WarrantyAlert:DaysBeforeExpiry"] ?? "30");
        var now = DateTime.UtcNow;
        var until = now.AddDays(days);

        var list = await db.WarrantyCards
            .Include(w => w.Product)!.ThenInclude(p => p!.Customer)
            .Where(w => w.IsActive && w.EndDate >= now && w.EndDate <= until)
            .ToListAsync(ct);

        foreach (var w in list)
        {
            var customerEmail = w.Product?.Customer?.Email;
            if (string.IsNullOrWhiteSpace(customerEmail)) continue;

            var daysLeft = (int)(w.EndDate - now).TotalDays;
            await email.SendAsync(
                customerEmail,
                $"[E-Warranty] Thẻ bảo hành sắp hết hạn ({daysLeft} ngày)",
                $"<p>Xin chào {w.Product?.Customer?.FullName},</p>" +
                $"<p>Thẻ bảo hành <b>{w.CardCode}</b> của thiết bị <b>{w.Product?.Name}</b> " +
                $"sẽ hết hạn vào <b>{w.EndDate:dd/MM/yyyy}</b> (còn {daysLeft} ngày).</p>");
        }

        _logger.LogInformation("Đã xử lý {Count} thẻ BH sắp hết hạn", list.Count);
    }
}