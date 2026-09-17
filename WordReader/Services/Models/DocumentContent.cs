using System.Net;
using System.Text;
using System.Text.RegularExpressions;



namespace WordReader.Services.Models;
public sealed class HistoricalDate
{
    public StandardDate Date { get; set; }
    public HistoricalDatePart? Hijri { get; set; }
    public HistoricalDatePart? Shamsi { get; set; }
    public HistoricalDatePart? Gregorian { get; set; }
}
public record StandardDate(bool IsNegative,DateOnly Date);
public enum HistoricalCalendarType
{
    Hijri,
    Shamsi,
    Gregorian
}

public sealed class HistoricalDatePart
{
    public HistoricalCalendarType CalendarType { get; set; }
    public int Day { get; set; }
    public int MonthNumber { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int Year { get; set; }

    // متن استاندارد خودت برای تاریخ‌های منفی
    public string NormalizedText =>
        $"{Year:D}{(MonthNumber >= 1 ? $"/{MonthNumber:D2}" : "")}/{Day:D2}";
}
public sealed class DocumentContent
{
    
    public string? FilePath { get; set; }

    public List<PageContent> Pages { get; set; } = new();

    public Dictionary<string, string> Styles { get; set; } = new();

    public List<IndexItem> Index { get; set; } = new();
    public string Title { get; set; }
}
public sealed class IndexItem
{
    public int RowNumber { get; set; }
    public string Title { get; set; }
    public string Date { get; set; }
    public int BookIndex { get; set; }
    public int StartPageNumber { get; set; }
    public int EndPageNumber { get; set; }
}
public sealed class PageContent
{
    public int PageNumber { get; set; }

    public string Text { get; set; } = string.Empty;

    public string HtmlContent { get; set; } = string.Empty;

    public List<ParagraphSpan> Paragraphs { get; set; } = new();
    public List<ImageContent> Images { get; set; } = new();
    public List<TableContent> Tables { get; set; } = new();
    public List<ParagraphSpan> Header { get; set; } = [];
    public List<ParagraphSpan> Footer { get; set; } = []; 
    public List<ParagraphSpan> Footnotes { get; set; } = new();



    public void RebuildHtml()
    {
        var sb = new StringBuilder();

        foreach (var paragraph in Paragraphs)
        {
            var cssClass = CssClassName.FromStyleName(paragraph.StyleName);
            var encodedText = WebUtility.HtmlEncode(paragraph.Text);

            sb.Append("<p");

            if (!string.IsNullOrWhiteSpace(cssClass))
                sb.Append(" class=\"").Append(cssClass).Append("\"");

            sb.Append(">");

            sb.Append(encodedText);

            sb.AppendLine("</p>");
        }

        if (Footnotes.Any())
        {
            sb.AppendLine("<section class=\"footnotes\">");

            foreach (var footnote in Footnotes)
            {
                var cssClass = CssClassName.FromStyleName(footnote.StyleName);
                var encodedText = WebUtility.HtmlEncode(footnote.Text);

                sb.Append("<p");

                if (!string.IsNullOrWhiteSpace(cssClass))
                    sb.Append(" class=\"").Append(cssClass).Append("\"");

                sb.Append(">");

                sb.Append(encodedText);

                sb.AppendLine("</p>");
            }

            sb.AppendLine("</section>");
        }

        HtmlContent = sb.ToString();
    }

}

public sealed class ParagraphSpan
{
    /// <summary>
    /// شماره صفحه‌ای که این تکه پاراگراف داخل آن قرار دارد.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// شماره پاراگراف اصلی در کل سند.
    /// اگر یک پاراگراف بین چند صفحه split شود، ParagraphIndex برای همه تکه‌ها یکسان می‌ماند.
    /// </summary>
    public int ParagraphIndex { get; set; }

    /// <summary>
    /// شماره تکه داخل همان پاراگراف.
    /// برای پاراگراف عادی معمولاً 1 است.
    /// برای پاراگراف چندصفحه‌ای ممکن است 1, 2, 3 و ...
    /// </summary>
    public int FragmentIndex { get; set; }

    public string Text { get; set; } = string.Empty;

    public string StyleName { get; set; } = string.Empty;

    public bool IsSplitFromMultiPageParagraph { get; set; }
    public bool IsFootnote { get; set; } = false;
    public FootnoteReferenceContext? FootnoteReferenceContext { get; set; }

    public string Date { get; set; }
    public string Ravi { get; set; }
    public string Story { get; set; }
    public string Fehrest { get; set; }
}
public sealed class FootnoteReferenceContext
{
    public string? ReferenceNumber { get; set; }
    public string? ReferenceWord { get; set; }
    public string? PrecedingWord1 { get; set; }
    public string? PrecedingWord2 { get; set; }
}

public sealed class ImageContent
{
    public int PageNumber { get; set; }

    public int ImageIndex { get; set; }

    public string ContentType { get; set; } = "image/png";

    public byte[] Bytes { get; set; } = Array.Empty<byte>();

    public string Base64 =>
        Bytes.Length == 0 ? string.Empty : Convert.ToBase64String(Bytes);
}
public sealed class TableContent
{
    public int PageNumber { get; set; }

    public int TableIndex { get; set; }

    public bool IsSplitFromMultiPageTable { get; set; }

    public int FirstRowIndexOnPage { get; set; }

    public int LastRowIndexOnPage { get; set; }

    public List<TableRowContent> Rows { get; set; } = new();
}
public sealed class TableRowContent
{
    public int RowIndex { get; set; }

    public List<TableCellContent> Cells { get; set; } = new();
}
public sealed class TableCellContent
{
    public int RowIndex { get; set; }

    public int ColumnIndex { get; set; }

    public string Text { get; set; } = string.Empty;
}



//public sealed class StyleName
//{
//    public StyleName(string name)
//    {
//        Name = name;
//    }

//    public string Name { get; set; } = string.Empty;

//    public string CssClassName => Models.CssClassName.FromStyleName(Name);
//}

public static class CssClassName
{
    public static string FromStyleName(string? styleName)
    {
        if (string.IsNullOrWhiteSpace(styleName))
            return string.Empty;

        var normalized = styleName.Trim().ToLowerInvariant();

        normalized = Regex.Replace(normalized, @"\s+", "-");
        normalized = Regex.Replace(normalized, @"[^a-z0-9\-_]", "-");
        normalized = Regex.Replace(normalized, @"-+", "-");
        normalized = normalized.Trim('-');

        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "style";

        return "style-" + normalized;
    }
}