using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace LandingPage.Pages;
public class PublicQuestionsModel : PageModel
{
    private static readonly List<FaqItem> AllQuestions = new()
{
    new FaqItem(
        "چگونه می‌توانم ارجاعات دقیق از جلدهای مختلف استخراج کنم؟",
        "در نسخه آنلاین، بخش «ابزار پژوهشی» را فعال کنید. هر بار که متن را انتخاب می‌کنید، پنجره‌ای با ذکر جلد، صفحه و شناسهٔ یکتا به شما داده می‌شود. این شناسه را می‌توانید به نرم‌افزار مدیریت منابع وارد کنید.",
        "پژوهش و استناد",
        new DateTime(2026, 6, 5)
    ),
    new FaqItem(
        "آیا امکان اشتراک گروهی برای اعضای یک پژوهشکده وجود دارد؟",
        "بله. فرم درخواست اشتراک گروهی را از طریق صفحه «تماس با ما» ارسال کنید. پس از بررسی، قرارداد ویژه با دسترسی همزمان برای اعضاء تنظیم می‌شود.",
        "اشتراک و دسترسی",
        new DateTime(2026, 5, 28)
    ),
    new FaqItem(
        "چطور می‌توانم گزارش خطای محتوایی ارائه دهم؟",
        "در صفحه خوانش، گزینه «گزارش خطا» کنار هر پاراگراف موجود است. همچنین می‌توانید از فرم تماس استفاده کنید و مرجع صحیح را ضمیمه نمایید.",
        "پشتیبانی محتوا",
        new DateTime(2026, 6, 2)
    ),
    new FaqItem(
        "آیا امکان دریافت نسخه آفلاین ۱۴ جلد وجود دارد؟",
        "فعلاً نسخه آفلاین به شکل کامل منتشر نشده است، اما می‌توانید بخش‌هایی از هر جلد را برای پژوهش شخصی با رعایت حقوق مالکیت دانلود کنید.",
        "دسترسی و ذخیره‌سازی",
        new DateTime(2026, 5, 18)
    ),
    new FaqItem(
        "چگونه می‌توانم از ابزار جستجوی معنایی بهترین استفاده را ببرم؟",
        "به جای جستجو بر اساس کلیدواژهٔ دقیق، از عبارت‌های توصیفی استفاده کنید؛ موتور جستجو ارتباط مفهومی را تحلیل کرده و بخش‌های مرتبط را پیشنهاد می‌کند.",
        "پژوهش و استناد",
        new DateTime(2026, 6, 1)
    )
};

    public IReadOnlyList<FaqItem> Items { get; private set; } = Array.Empty<FaqItem>();
    public List<string> Categories { get; private set; } = new();
    public string? CategoryFilter { get; private set; }
    public string LastUpdatedHumanized { get; private set; } = string.Empty;

    public void OnGet(string? category)
    {
        Categories = AllQuestions
            .Select(q => q.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        CategoryFilter = string.IsNullOrWhiteSpace(category) ? null : category;

        var filtered = AllQuestions
            .Where(q => CategoryFilter == null || q.Category == CategoryFilter)
            .OrderByDescending(q => q.UpdatedOn)
            .ToList();

        Items = filtered;

        var latestDate = filtered.Any()
            ? filtered.First().UpdatedOn
            : AllQuestions.Max(q => q.UpdatedOn);

        LastUpdatedHumanized = latestDate.ToString("dddd d MMMM yyyy", new CultureInfo("fa-IR"));
    }

}
    public record FaqItem(string Question, string Answer, string Category, DateTime UpdatedOn);
