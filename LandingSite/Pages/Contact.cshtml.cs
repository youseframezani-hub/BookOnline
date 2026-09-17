using LandingPage.ModelsAndServices.Contacts.Models;
using LandingPage.ModelsAndServices.Contacts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LandingPage.Pages;

public class ContactModel : PageModel
{
    private readonly IContactMessageService _contactService;

    public ContactModel(IContactMessageService contactService)
    {
        _contactService = contactService;
    }

    [BindProperty]
    public ContactMessage ContactMessage { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        await _contactService.SaveContactMessageAsync(ContactMessage);

        TempData["Success"] = "پیام شما با موفقیت ثبت شد. پس از بررسی توسط ادمین نمایش داده خواهد شد.";
        return RedirectToPage("/Contact");
    }
}
