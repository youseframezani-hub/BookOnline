using System.Text.Json;
using System.Text.Json.Serialization;
using BookImporter.Persistence.Entities;
using BookOnline.Domain.Books;
using WordReader.Services.Models;

namespace BookImporter.Persistence;

public static class JsonConfiguration
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}

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

    public void SaveStyles(IList<BookStyleEntity> styles)
    {
        for (var i = 0; i < styles.Count; i++)
        {
            var sp = styles[i].StyleParagraphProperties;
            var rp = styles[i].StyleRunProperties;
            if (rp != null)
            {
                _db.StyleRunProperties.Add(rp);
                _db.SaveChanges();
                styles[i].StyleRunPropertiesId = rp.Id;
            }
            if (sp != null)
            {
                _db.StyleParagraphProperties.Add(sp);
                _db.SaveChanges();
                styles[i].StyleParagraphPropertiesId = sp.Id;
            }

            _db.Styles.Add(styles[i]);
        }

        _db.SaveChanges();
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

            foreach (ParagraphSection paragraph in page.PragraphSections)
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