using LandingPage.ModelsAndServices.Contacts.Models;
using LandingPage.ModelsAndServices.Contacts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LandingPage.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IUserRegistrationService _userRegistrationService;

        public IndexModel(ILogger<IndexModel> logger, IUserRegistrationService userRegistrationService)
        {
            _logger = logger;
            _userRegistrationService = userRegistrationService;
        }

        [BindProperty]
        public UserRegistration UserRegistration { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            bool success = await _userRegistrationService.RegisterUserAsync(UserRegistration);

            if (success)
            {
                TempData["SignupSuccess"] = "✅ ثبت‌نام با موفقیت انجام شد. لطفاً ایمیل خود را بررسی کنید.";
                return RedirectToPage("", "", "join");
            }
            else
            {
                TempData["SignupError"] = "❌ ایمیل یا شماره موبایل قبلاً ثبت شده است.";
                return RedirectToPage("", "", "join");
                //ModelState.AddModelError("UserRegistration.Email", "ایمیل یا شماره موبایل قبلاً ثبت شده است.");
                //return Page();
            }
        }
    }
}
