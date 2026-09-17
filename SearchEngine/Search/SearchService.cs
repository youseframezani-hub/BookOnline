using SearchEngine.Indexing;
using SearchEngine.Models;

namespace SearchEngine.Search;

public sealed class SearchService(PositionalInvertedIndex index)
{
    /// <summary>جستجوی عبارتی: همهٔ واژه‌ها با فاصلهٔ ۱ و به‌ترتیب</summary>
    public IEnumerable<SearchResult> PhraseSearch(IReadOnlyList<string> terms)
    {
        if (terms.Count == 0) yield break;

        if (terms.Count == 1)
        {
            foreach (var posting in index.GetPostings(terms[0]))
                yield return BuildResult(posting.ParagraphId);
            yield break;
        }

        var lists = terms.Select(index.GetPostings).ToList();
        if (lists.Any(l => l.Count == 0)) yield break;

        var candidates = lists[0].Select(p => p.ParagraphId)
            .Intersect(lists[1].Select(p => p.ParagraphId));

        foreach (var pid in candidates)
            if (HasSequentialPhrase(lists.Select(l =>
                    l.First(p => p.ParagraphId == pid).Positions).ToList()))
                yield return BuildResult(pid);
    }

    private static bool HasSequentialPhrase(List<List<int>> lists) =>
        lists[0].Any(start => Enumerable.Range(1, lists.Count - 1)
            .All(i => lists[i].BinarySearch(start + i) >= 0));

    /// <summary>جستجوی مجاورتی: واژه‌ها در پنجرهٔ maxGap واژه از هم</summary>
    public IEnumerable<SearchResult> ProximitySearch(string t1, string t2, int maxGap)
    {
        var p1 = index.GetPostings(t1);
        var p2 = index.GetPostings(t2);

        int i = 0, j = 0;                    // Two-pointer کلاسیک روی ParagraphId
        while (i < p1.Count && j < p2.Count)
        {
            if (p1[i].ParagraphId == p2[j].ParagraphId)
            {
                if (HasClosePositions(p1[i].Positions, p2[j].Positions, maxGap))
                    yield return BuildResult(p1[i].ParagraphId);
                i++; j++;
            }
            else if (p1[i].ParagraphId < p2[j].ParagraphId) i++;
            else j++;
        }
    }

    private static bool HasClosePositions(List<int> a, List<int> b, int maxGap)
    {
        int i = 0, j = 0;                    // Two-pointer روی موضع‌های مرتب
        while (i < a.Count && j < b.Count)
        {
            if (Math.Abs(a[i] - b[j]) <= maxGap) return true;
            if (a[i] < b[j]) i++; else j++;
        }
        return false;
    }

    private SearchResult BuildResult(int paragraphId)
    {
        var (bookId, pageNumber) = index.GetParagraphMeta(paragraphId);
        return new SearchResult
        {
            ParagraphId = paragraphId,
            BookId = bookId,
            PageNumber = pageNumber,
            RawText = index.GetParagraphText(paragraphId)
        };
    }
}
