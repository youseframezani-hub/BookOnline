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
        _paragraphTexts[paragraphId] = rawText;
        _paragraphMeta[paragraphId] = (bookId, pageNumber);

        foreach (var (term, position, offset) in normalizer.Tokenize(rawText))
        {
            var postings = _data.GetOrAdd(term, _ => new List<PostingEntry>());
            lock (postings)
            {
                if (postings.Count == 0 || postings[^1].ParagraphId != paragraphId)
                    postings.Add(new PostingEntry
                    {
                        ParagraphId = paragraphId,
                        BookId = bookId,
                        PageNumber = pageNumber
                    });

                postings[^1].AddPosition(position, offset);
            }
        }
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
}
