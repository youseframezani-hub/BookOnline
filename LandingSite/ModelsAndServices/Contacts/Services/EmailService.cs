using Microsoft.Extensions.Options;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using System.Net;

namespace LandingPage.ModelsAndServices.Contacts.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendEmailToMultipleAsync(List<string> toList, string subject, string body, bool isHtml = true);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("ایمیل گیرنده نمی‌تواند خالی باشد.");

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        mailMessage.To.Add(to);

        using var smtpClient = new SmtpClient
        {
            Host = _settings.SmtpServer,
            Port = _settings.Port,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = _settings.UseSsl,
            Timeout = 30000 // ۳۰ ثانیه
        };

        try
        {
            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, $"خطا در ارسال ایمیل: {ex.Message}");
            throw;
        }
    }

    public async Task SendEmailToMultipleAsync(List<string> toList, string subject, string body, bool isHtml = true)
    {
        foreach (var email in toList.Where(e => !string.IsNullOrWhiteSpace(e)))
        {
            try
            {
                await SendEmailAsync(email, subject, body, isHtml);
            }
            catch (Exception ex)
            {
                // ادامه ارسال برای بقیه ایمیل‌ها حتی اگر یکی خطا داد
                _logger.LogError(ex, $"خطا در ارسال به {email}: {ex.Message}");
            }
        }
    }
}
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "سیره نبوی";
    public bool UseSsl { get; set; } = true;
}