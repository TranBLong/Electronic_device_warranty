
// EmailService.cs
using System.Net;
using System.Net.Mail;

namespace EWarrantySystem.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var host = _config["Email:SmtpHost"];
        var from = _config["Email:From"];
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(from))
        {
            _logger.LogWarning("Email chưa cấu hình — bỏ qua gửi tới {To}", to);
            return;
        }

        using var client = new SmtpClient(host, int.Parse(_config["Email:SmtpPort"] ?? "587"))
        {
            EnableSsl = bool.Parse(_config["Email:EnableSsl"] ?? "true"),
            Credentials = new NetworkCredential(
                _config["Email:Username"],
                _config["Email:Password"])
        };

        var msg = new MailMessage(from, to, subject, body) { IsBodyHtml = true };
        await client.SendMailAsync(msg);
    }
}