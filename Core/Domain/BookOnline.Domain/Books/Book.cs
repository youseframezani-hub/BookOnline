using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BookOnline.Domain.Books.MatchiurityScoreFactory;
using static System.Net.Mime.MediaTypeNames;

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

public class BookIndexItem
{
    public BookIndexItemId Id { get; }
    public string Title { get; set; }
    public PageMark PageMark { get; set; }
}
public class BookIndex
{
    private List<BookIndexItem> _items;
    public IReadOnlyList<BookIndexItem> Items =>
        _items.OrderBy(o => o.Id.RowNumber).ToList();
    public IReadOnlyList<BookIndexItem> SearchByTitle(string title) =>
        _items.Where(i => i.Title.Contains(title.Trim())).ToList();
    public PageMark? GetById(BookIndexItemId id) =>
        _items.FirstOrDefault(i => i.Id == id)?.PageMark;
}

public record PageMark(PageId Start, PageId End);
public record Info(string Title);
public record BookIndexItemId(BookId BookId, int RowNumber);
public record PageId(BookId BookId, int PageNumber);
public class IPage
{
    public PageId Id { get; }
    public string HtmlContent { get; set; }
    public List<PragraphSection> PragraphSections { get; }

    public bool Contain(string value) =>
        PragraphSections.Any(p => p.Contain(value));

    public bool Contain(PragraphSectionType type, string value) =>
        PragraphSections.Any(p => p.Contain(type, value));
}

public class PragraphSection
{
    public BookId PageId { get; }
    public PragraphSectionType Type { get; }
    public SectionHmlStyle Style { get; }
    public List<AtomicBlockSection> AtomicBlockSections { get; }

    public bool Contain(string value) =>
        AtomicBlockSections.Any(a => a.Contain(value.Trim()));

    public bool Contain(PragraphSectionType type, string value) =>
        Type == type && Contain(value);
}
public class AtomicBlockSection
{
    public string Worde { get; set; }
    public int Length => Worde.Length;
    public PageId PageId { get; set; }
    public int NumberOfIndexPage { get; set; }
    public int NextWordCountPage { get; set; }
    public List<string> NextWodrs { get; set; }
    public List<string> PreviousWodrs { get; set; }
    public bool Contain(string value) => this.Contains(value) == MatchiurityScore.FitWord;
    public MatchiurityScore Contains(string value)
    {

        var tokenized = Tokenize(value);
        byte p = 0, n = 0;
        bool fitword = false;
        foreach (var item in tokenized)
        {
            if (NextWodrs.Contains(item))
                n++;
            if (PreviousWodrs.Contains(item))
                p++;
            fitword = item.Equals(Worde);
        }
        return MatchiurityScoreFactory.Create(fitword, n, p, (byte)tokenized.Length);
    }

    private static string[] Tokenize(string value)
    {
        return value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

public static class MatchiurityScoreFactory
{
    public static MatchiurityScore Create(bool fitWord, byte nextWordMatchedCount, byte previousWordMatchedCount, byte tokenCont)
    {
        MatchiurityScore score;
        score = !fitWord && (previousWordMatchedCount == 0 || nextWordMatchedCount == 0) ? MatchiurityScore.Contain : MatchiurityScore.None;
        score = fitWord && previousWordMatchedCount == 0 && nextWordMatchedCount == 0 ? MatchiurityScore.FitWord : MatchiurityScore.None;
        score = !fitWord && (previousWordMatchedCount > 0 && nextWordMatchedCount > 0) ? MatchiurityScore.Normal : MatchiurityScore.None;
        score = fitWord && (previousWordMatchedCount > 0 || nextWordMatchedCount > 0) ? MatchiurityScore.Best : MatchiurityScore.None;
        score = fitWord && (previousWordMatchedCount > 0 && nextWordMatchedCount > 0) ? MatchiurityScore.Grate : MatchiurityScore.None;
        score = previousWordMatchedCount + nextWordMatchedCount + Convert.ToByte(fitWord) == tokenCont ? MatchiurityScore.FullFit : MatchiurityScore.None;
        return score;
    }

    public enum MatchiurityScore
    {
        None,
        Contain = 1,
        FitWord,
        Normal,
        Best,
        Grate,
        FullFit
    }
}
class AtomicBlockSectionHtmlRender
{
    public string HtmlRender(AtomicBlockSection atomicBlock)
    {
        return $"<{(atomicBlock.Style.LineType == LineType.Inline ? "span" : "p")}> + {atomicBlock.Value}+</{(atomicBlock.Style.LineType == LineType.Inline ? "span" : "p")}>";
    }
}
class PragraphSectionHtmlRender
{
    private AtomicBlockSectionHtmlRender _atomicRender;
    public string HtmlRender(PragraphSection pragraph)
    {
        return $"<section> + {pragraph.AtomicBlockSections.Select(s => _atomicRender.HtmlRender(s))}+</section>";
    }
}
class PageHtmlRender
{
    private PragraphSectionHtmlRender _atomicRender;
    string HtmlRender(IPage pragraph)
    {
        return $"<html><body> + {pragraph.PragraphSections.Select(s => _atomicRender.HtmlRender(s))}+</body></html>";
    }
}
public class SectionHmlStyle
{
    //text-align: center;
    //direction: ltr;
    //font-weight: 500;
    //color: #ff12dd;
    //text-decoration: blink;

    public LineType LineType { get; }
    public FontSize FontSize { get; }
}
public record FontSize(int Value, FontUnit Unit);
public class BookId
{
}
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

