using System.ComponentModel.DataAnnotations;

namespace LandingPage.ModelsAndServices.Contacts.Models;

public class ContactMessage
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;
    public bool IsApproved { get; set; } = false;   // برای نمایش در بخش پرسش‌های عمومی
    public string? AdminReply { get; set; }
}
