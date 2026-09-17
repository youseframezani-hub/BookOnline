using System.ComponentModel.DataAnnotations;

namespace LandingPage.ModelsAndServices.Contacts.Models;

public class UserRegistration
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است")]
    [StringLength(100)]
    [Display(Name = "نام و نام خانوادگی")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "ایمیل الزامی است")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر وارد کنید")]
    [Display(Name = "ایمیل")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل الزامی است")]
    [Phone(ErrorMessage = "شماره موبایل معتبر وارد کنید")]
    [StringLength(11)]
    [Display(Name = "شماره موبایل")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "نوع کاربری الزامی است")]
    [Display(Name = "نوع کاربری")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور الزامی است")]
    [MinLength(6, ErrorMessage = "رمز عبور حداقل باید ۶ کاراکتر باشد")]
    [Display(Name = "رمز عبور")]
    public string Password { get; set; } = string.Empty;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public bool IsConfirmed { get; set; } = false;
}