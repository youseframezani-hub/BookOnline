using System.Collections.Concurrent;
using SearchEngine.Models;

namespace SearchEngine.Indexing;

public sealed class PositionalInvertedIndex
{
    private readonly ConcurrentDictionary<string, List<PostingEntry>> _data = new();
    private readonly ConcurrentDictionary<int, string> _paragraphTexts = new();
    private readonly ConcurrentDictionary<int, (int BookId, int PageNumber)> _paragraphMeta = new();

    public int ParagraphCount => _paragraphTexts.Count;

    public void AddParagraph(int paragraphId, int bookId, int pageNumber,
                             string rawText, Analysis.PersianNormalizer normalizer)
    {
        SaveParagraphInfo(paragraphId, bookId, pageNumber, rawText);

        foreach (var (term, position, offset) in normalizer.Tokenize(rawText))
            AddTermOccurrence(term, position, offset, paragraphId, bookId, pageNumber);
    }

    public IReadOnlyList<PostingEntry> GetPostings(string term)
        => _data.TryGetValue(term, out var list)
            ? list.AsReadOnly()
            : Array.Empty<PostingEntry>();

    public string GetParagraphText(int paragraphId)
        => _paragraphTexts.TryGetValue(paragraphId, out var text) ? text : string.Empty;

    /// <summary>
    /// اطلاعات Facet (شناسهٔ کتاب و شمارهٔ صفحه) یک پاراگراف را برمی‌گرداند.
    /// چون <see cref="SearchResult"/> این دو فیلد را required دارد، سرویس جستجو
    /// برای ساخت نتیجه به این متد نیاز دارد.
    /// </summary>
    public (int BookId, int PageNumber) GetParagraphMeta(int paragraphId)
        => _paragraphMeta.TryGetValue(paragraphId, out var meta) ? meta : (0, 0);

    // ---------- کمکی‌های افزودن پاراگراف ----------

    private void SaveParagraphInfo(int paragraphId, int bookId, int pageNumber, string rawText)
    {
        _paragraphTexts[paragraphId] = rawText;
        _paragraphMeta[paragraphId] = (bookId, pageNumber);
    }

    private void AddTermOccurrence(string term, int position, int offset,
                                   int paragraphId, int bookId, int pageNumber)
    {
        var postings = _data.GetOrAdd(term, _ => new List<PostingEntry>());

        lock (postings)
        {
            if (NeedsNewPosting(postings, paragraphId))
                postings.Add(CreatePosting(paragraphId, bookId, pageNumber));

            var currentPosting = postings[postings.Count - 1];
            currentPosting.AddPosition(position, offset);
        }
    }

    /// اگر لیست خالی است یا آخرین Posting مال پاراگراف دیگری است، باید Posting تازه ساخت
    private static bool NeedsNewPosting(List<PostingEntry> postings, int paragraphId)
    {
        if (postings.Count == 0) return true;

        var lastPosting = postings[postings.Count - 1];
        return lastPosting.ParagraphId != paragraphId;
    }

    private static PostingEntry CreatePosting(int paragraphId, int bookId, int pageNumber)
        => new PostingEntry
        {
            ParagraphId = paragraphId,
            BookId = bookId,
            PageNumber = pageNumber
        };
}