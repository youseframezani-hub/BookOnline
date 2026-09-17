namespace BookOnline.Domain.Books;

public interface IBook
{
    int Id { get; }
    Info Info { get; }
    BookIndex Index { get; }
    IReadOnlyList<IPage> Pages { get; }
    IReadOnlyList<IPage> GetByIds(int[] Ids);

    IReadOnlyList<IPage> Contain(string value);

    IReadOnlyList<IPage> Contain(PragraphSectionType type, string value);
}

/// <summary>
/// پیاده‌سازی مرجع <see cref="IBook"/>. توسط BookImporter از خروجی WordReader ساخته می‌شود.
/// </summary>
public sealed class Book : IBook
{
    public Book(int id, Info info, BookIndex index, IReadOnlyList<IPage> pages)
    {
        Id = id;
        Info = info;
        Index = index;
        Pages = pages;
    }

    public int Id { get; }
    public Info Info { get; }
    public BookIndex Index { get; }
    public IReadOnlyList<IPage> Pages { get; }

    public IReadOnlyList<IPage> GetByIds(int[] Ids) =>
        Pages.Where(p => Ids.Contains(p.Id.PageNumber)).ToList();

    public IReadOnlyList<IPage> Contain(string value) =>
        Pages.Where(p => p.Contain(value)).ToList();

    public IReadOnlyList<IPage> Contain(PragraphSectionType type, string value) =>
        Pages.Where(p => p.Contain(type, value)).ToList();
}

public sealed class BookIndexItem
{
    public required BookIndexItemId Id { get; init; }
    public required string Title { get; init; }
    public PageMark? PageMark { get; init; }
}

public sealed class BookIndex
{
    private readonly List<BookIndexItem> _items;

    public BookIndex(IEnumerable<BookIndexItem> items)
    {
        _items = items.ToList();
    }

    public IReadOnlyList<BookIndexItem> Items =>
        _items.OrderBy(o => o.Id.RowNumber).ToList();
    public IReadOnlyList<BookIndexItem> SearchByTitle(string title) =>
        _items.Where(i => i.Title.Contains(title.Trim())).ToList();
    public PageMark? GetById(BookIndexItemId id) =>
        _items.FirstOrDefault(i => i.Id == id)?.PageMark;
}

public record BookId(int Value);
public record PageMark(PageId Start, PageId End);
public record Info(string Title);
public record BookIndexItemId(BookId BookId, int RowNumber);
public record PageId(BookId BookId, int PageNumber);

public sealed class IPage
{
    public required PageId Id { get; init; }
    public string HtmlContent { get; init; } = string.Empty;
    public required IReadOnlyList<PragraphSection> PragraphSections { get; init; }

    public bool Contain(string value) =>
        PragraphSections.Any(p => p.Contain(value));

    public bool Contain(PragraphSectionType type, string value) =>
        PragraphSections.Any(p => p.Contain(type, value));
}

/// <summary>
/// واحد یک پاراگراف در یک صفحه. طبق معماری جدید موتور جستجو (پروژهٔ SearchEngine)،
/// «منبع حقیقت» متن خام (<see cref="RawText"/>) است؛ ایندکس‌سازی واژه‌به‌واژه دیگر
/// در این کلاس نگه‌داری نمی‌شود (پیشتر AtomicBlockSection بود که به SearchEngine منتقل شد).
/// </summary>
public sealed class PragraphSection
{
    public required PageId PageId { get; init; }
    public required PragraphSectionType Type { get; init; }
    public SectionHmlStyle? Style { get; init; }
    public required string RawText { get; init; }

    public bool Contain(string value) =>
        !string.IsNullOrWhiteSpace(RawText) &&
        RawText.Contains(value.Trim(), StringComparison.OrdinalIgnoreCase);

    public bool Contain(PragraphSectionType type, string value) =>
        Type == type && Contain(value);
}

public sealed class SectionHmlStyle
{
    //text-align: center;
    //direction: ltr;
    //font-weight: 500;
    //color: #ff12dd;
    //text-decoration: blink;

    public LineType LineType { get; init; } = LineType.Block;
    public FontSize? FontSize { get; init; }
}
public record FontSize(int Value, FontUnit Unit);
public enum FontUnit
{
    Px,
    Inc,
    Xi,
    Percent
}
public enum LineType
{
    Inline,
    Block
}
public record PragraphSectionType(string Key,string Title);
