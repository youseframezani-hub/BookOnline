using BookOnline.Domain.Books;
using WordReader.Services.Models;

namespace BookImporter.Mapping;

/// <summary>
/// خروجی زندهٔ <see cref="ITextExtractor"/> (یعنی <see cref="DocumentContent"/>) را به مدل دامنهٔ
/// BookOnline.Domain.Books (<see cref="Book"/>/<see cref="IPage"/>/<see cref="PragraphSection"/>)
/// نگاشت می‌دهد. قاعدهٔ استخراج فهرست مطالب («TOC 1») همان منطقی است که پیشتر در
/// WordReader/Program.cs (به‌صورت اختصاصی برای این کتاب) پیاده شده بود؛ چون BookImporter
/// دیگر از آن Program.cs عبور نمی‌کند، همین‌جا بازتولید شده است.
/// </summary>
public sealed class DocumentToBookMapper
{
    public Book Map(DocumentContent document, int bookId)
    {
        var domainBookId = new BookId(bookId);

        var pages = document.Pages
            .Select(page => MapPage(page, domainBookId))
            .ToList();

        var index = BuildIndex(document, domainBookId);
        var title = BuildTitle(document);

        return new Book(bookId, new Info(title), new BookIndex(index), pages);
    }

    private static IPage MapPage(PageContent page, BookId bookId)
    {
        var pageId = new PageId(bookId, page.PageNumber);

        var paragraphs = page.Paragraphs
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .Select(p => new PragraphSection
            {
                PageId = pageId,
                Type = new PragraphSectionType(NormalizeKey(p.StyleName), p.StyleName),
                RawText = p.Text
            })
            .ToList();

        return new IPage
        {
            Id = pageId,
            HtmlContent = page.HtmlContent,
            PragraphSections = paragraphs
        };
    }

    private static string NormalizeKey(string styleName) =>
        string.IsNullOrWhiteSpace(styleName) ? "normal" : styleName.Trim();

    private static string BuildTitle(DocumentContent document)
    {
        var fallback = Path.GetFileNameWithoutExtension(document.FilePath) is { Length: > 0 } name
            ? name
            : "بدون عنوان";

        var firstPage = document.Pages.FirstOrDefault();
        if (firstPage is null)
            return fallback;

        var titleParagraphs = firstPage.Paragraphs
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .Select(p => p.Text);

        var title = string.Join("، ", titleParagraphs);
        return string.IsNullOrWhiteSpace(title) ? fallback : title;
    }

    private static List<BookIndexItem> BuildIndex(DocumentContent document, BookId bookId)
    {
        var items = new List<BookIndexItem>();
        var rowNumber = 0;

        foreach (var paragraph in document.Pages.SelectMany(p => p.Paragraphs))
        {
            if (!string.Equals(paragraph.StyleName?.Trim(), "TOC 1", StringComparison.Ordinal))
                continue;

            rowNumber++;

            // فرمت نمونه: "25 شعبانِ 190-/ مراحل سه‌گانۀ تسلط خُزاعه بر کعبه و حرم تا تصدیِ قُصَیّ بْن کِلاب\t95"
            // یعنی: [0]=تاریخ، [1]=عنوان، [2]=شمارهٔ صفحه
            var parts = paragraph.Text.Split(
                new[] { "\t", "/" },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var title = parts.Length > 1 ? parts[1] : parts.FirstOrDefault() ?? paragraph.Text;
            var pageNumber = parts.Length > 2 && int.TryParse(parts[2], out var parsedPage)
                ? parsedPage
                : paragraph.PageNumber;

            items.Add(new BookIndexItem
            {
                Id = new BookIndexItemId(bookId, rowNumber),
                Title = title,
                PageMark = new PageMark(
                    new PageId(bookId, pageNumber),
                    new PageId(bookId, pageNumber))
            });
        }

        return items;
    }
}
