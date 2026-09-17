namespace LandingPage.ModelsAndServices.Contacts.Services;

public interface ISmsService
{
    Task SendSmsAsync(string phoneNumber, string message);
}
public class SmsService : ISmsService
{
    // بعداً Kavenegar, Melipayamak و غیره اضافه کنید
    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        // TODO: Implement real SMS provider
        Console.WriteLine($"[SMS] To: {phoneNumber} | Message: {message}");
        await Task.CompletedTask;
    }
}

