namespace BookImporter.Persistence.Entities;

public sealed class PageEntity
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int PageNumber { get; set; }

    /// <summary>متن خام کل صفحه (چسبانده‌شده از پاراگراف‌های آن).</summary>
    public string RawText { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;

    public BookEntity? Book { get; set; }
    public List<ParagraphEntity> Paragraphs { get; set; } = new();
}
