using LandingPage.ModelsAndServices.Contacts.Data;
using LandingPage.ModelsAndServices.Contacts.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options;


namespace LandingPage.ModelsAndServices.Contacts.Services;

public interface IContactMessageService
{
    Task SaveContactMessageAsync(ContactMessage message);
}

public class ContactMessageService : IContactMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly AdminNotificationSettings _adminSettings;
    private readonly ILogger<ContactMessageService> _logger;

    public ContactMessageService(
        ApplicationDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        IOptions<AdminNotificationSettings> adminSettings,
        ILogger<ContactMessageService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _adminSettings = adminSettings.Value;
        _logger = logger;
    }

    public async Task SaveContactMessageAsync(ContactMessage message)
    {
        _context.ContactMessages.Add(message);
        //await _context.SaveChangesAsync();

        try
        {
            // ارسال ایمیل به ادمین‌ها
            if (_adminSettings.AdminEmails?.Any() == true)
            {
                string emailBody = $@"
                    <h3>پیام جدید از فرم تماس</h3>
                    <p><strong>نام:</strong> {message.FullName}</p>
                    <p><strong>ایمیل:</strong> {message.Email}</p>
                    <p><strong>موضوع:</strong> {message.Subject}</p>
                    <p><strong>پیام:</strong></p>
                    <p style='background:#f8f9fa; padding:15px; border-radius:8px;'>{message.Message}</p>
                    <hr>
                    <small>زمان: {message.CreatedAt:yyyy/MM/dd HH:mm}</small>";

                await _emailService.SendEmailToMultipleAsync(
                    _adminSettings.AdminEmails,
                    $"پیام جدید تماس - {message.Subject}",
                    emailBody);
            }

            // ارسال نوتیفیکیشن SMS به ادمین (اختیاری)
            if (!string.IsNullOrEmpty(_adminSettings.AdminPhone))
            {
                await _smsService.SendSmsAsync(
                    _adminSettings.AdminPhone,
                    $"پیام جدید در فرم تماس سیره نبوی. لطفا ایمیل را چک کنید.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}

public class AdminNotificationSettings
{
    public List<string> AdminEmails { get; set; } = new();
    public string? AdminPhone { get; set; }
}