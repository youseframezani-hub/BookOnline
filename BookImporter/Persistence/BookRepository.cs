using BookImporter.Persistence.Entities;
using BookOnline.Domain.Books;

namespace BookImporter.Persistence;

/// <summary>
/// ذخیرهٔ یک <see cref="IBook"/> دامنه در دیتابیس، صرفاً به‌صورت متن خام.
/// هیچ منطق دامنه‌ای اینجا تکرار نمی‌شود؛ فقط یک ترجمهٔ مستقیم Domain → Entity.
/// </summary>
public sealed class BookRepository
{
    private readonly ImportDbContext _db;

    public BookRepository(ImportDbContext db)
    {
        _db = db;
    }

    public int SaveBook(IBook book, string sourceFilePath)
    {
        var bookEntity = new BookEntity
        {
            Title = book.Info.Title,
            SourceFilePath = sourceFilePath
        };

        foreach (var page in book.Pages)
        {
            var pageEntity = new PageEntity
            {
                PageNumber = page.Id.PageNumber,
                RawText = string.Join(Environment.NewLine, page.PragraphSections.Select(p => p.RawText)),
                HtmlContent = page.HtmlContent
            };

            foreach (var paragraph in page.PragraphSections)
            {
                pageEntity.Paragraphs.Add(new ParagraphEntity
                {
                    StyleName = paragraph.Type.Title,
                    RawText = paragraph.RawText
                });
            }

            bookEntity.Pages.Add(pageEntity);
        }

        _db.Books.Add(bookEntity);
        _db.SaveChanges();

        return bookEntity.Id;
    }
}
