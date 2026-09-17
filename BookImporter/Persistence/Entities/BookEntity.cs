namespace BookImporter.Persistence.Entities;

/// <summary>
/// موجودیت خالص EF Core (بدون هیچ وابستگی به ABP) برای ذخیرهٔ یک کتاب.
/// داده‌ها به‌صورت متن خام (Raw Text) نگه‌داری می‌شوند؛ منبع حقیقت همین جدول‌هاست.
/// </summary>
public sealed class BookEntity
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? SourceFilePath { get; set; }
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;

    public List<PageEntity> Pages { get; set; } = new();
}
