using LandingPage.ModelsAndServices.Contacts.Data;
using LandingPage.ModelsAndServices.Contacts.Models;
//using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;

namespace LandingPage.ModelsAndServices.Contacts.Services;

public interface IUserRegistrationService
{
    Task<bool> RegisterUserAsync(UserRegistration model);
}
public class UserRegistrationService : IUserRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly AdminNotificationSettings _adminSettings;
    private readonly ILogger<UserRegistrationService> _logger;

    public UserRegistrationService(ApplicationDbContext context, IEmailService emailService, ISmsService smsService, IOptions<AdminNotificationSettings> adminSettings, ILogger<UserRegistrationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _adminSettings = adminSettings.Value;
        _logger = logger;
    }

    public async Task<bool> RegisterUserAsync(UserRegistration model)
    {
        // چک تکراری بودن ایمیل
        //if (await _context.UserRegistrations.AnyAsync(u => u.Email == model.Email))
        //    return false;

        if (_context.UserRegistrations.Any(u => u.Email == model.Email || u.Mobile == model.Mobile))
            return false;


        _context.UserRegistrations.Add(model);
        //await _context.SaveChangesAsync();

        // ==================== ارسال نوتیفیکیشن ====================
        try
        {

            // ۱. ایمیل خوش‌آمدگویی به کاربر
            string userEmailBody = $@"
                <h3>خوش آمدید {model.FullName} عزیز!</h3>
                <p>ثبت‌نام شما در فروشگاه سیره نبوی با موفقیت انجام شد.</p>
                <p>ایمیل: <strong>{model.Email}</strong></p>
                <p>شماره موبایل: <strong>{model.Mobile}</strong></p>
                <hr>
                <p>حالا می‌توانید از امکانات سایت استفاده کنید.</p>";

            await _emailService.SendEmailAsync(model.Email, "ثبت‌نام موفق - سیره نبوی", userEmailBody);

            // ۲. نوتیفیکیشن به ادمین‌ها (مثل فرم تماس)
            if (_adminSettings.AdminEmails?.Any() == true)
            {
                string adminBody = $@"
                    <h3>ثبت‌نام جدید</h3>
                    <p><strong>نام:</strong> {model.FullName}</p>
                    <p><strong>ایمیل:</strong> {model.Email}</p>
                    <p><strong>موبایل:</strong> {model.Mobile}</p>
                    <p><strong>نوع کاربری:</strong> {model.Role}</p>
                    <small>زمان ثبت: {model.RegisteredAt:yyyy/MM/dd HH:mm}</small>";

                await _emailService.SendEmailToMultipleAsync(
                    _adminSettings.AdminEmails,
                    "ثبت‌نام جدید کاربر",
                    adminBody);
            }

            // ۳. ارسال SMS به ادمین (نوتیفیکیشن)
            if (!string.IsNullOrEmpty(_adminSettings.AdminPhone))
            {
                await _smsService.SendSmsAsync(
                    _adminSettings.AdminPhone,
                    $"ثبت‌نام جدید: {model.FullName} - {model.Mobile}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,ex.Message);
        }
        return true;
    }
}
