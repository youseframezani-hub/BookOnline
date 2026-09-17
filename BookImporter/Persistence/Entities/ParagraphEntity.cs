namespace BookImporter.Persistence.Entities;

public sealed class ParagraphEntity
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public string StyleName { get; set; } = string.Empty;

    /// <summary>متن خام پاراگراف؛ همان چیزی که موتور جستجو نیز مستقیماً ایندکس می‌کند.</summary>
    public string RawText { get; set; } = string.Empty;

    public PageEntity? Page { get; set; }
}
