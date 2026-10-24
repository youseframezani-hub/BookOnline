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
            foreach (var result in SearchSingleTerm(terms[0]))
                yield return result;
            yield break;
        }

        var postingsPerTerm = GetPostingsForEachTerm(terms);
        if (AnyTermHasNoPostings(postingsPerTerm)) yield break;

        var lookupPerTerm = BuildLookupForEachTerm(postingsPerTerm);
        var candidateIds = FindParagraphIdsContainingAllTerms(postingsPerTerm[0], lookupPerTerm);

        foreach (var paragraphId in candidateIds)
        {
            var positionsPerTerm = GetPositionsForEachTerm(lookupPerTerm, paragraphId);
            if (HasSequentialPhrase(positionsPerTerm))
                yield return BuildResult(paragraphId);
        }
    }

    /// <summary>جستجوی مجاورتی: واژه‌ها در پنجرهٔ maxGap واژه از هم</summary>
    public IEnumerable<SearchResult> ProximitySearch(string firstTerm, string secondTerm, int maxGap)
    {
        var firstPostings = index.GetPostings(firstTerm);
        var secondPostings = index.GetPostings(secondTerm);

        int firstIndex = 0;
        int secondIndex = 0;

        while (firstIndex < firstPostings.Count && secondIndex < secondPostings.Count)
        {
            var first = firstPostings[firstIndex];
            var second = secondPostings[secondIndex];

            if (first.ParagraphId == second.ParagraphId)
            {
                if (HasClosePositions(first.Positions, second.Positions, maxGap))
                    yield return BuildResult(first.ParagraphId);

                firstIndex++;
                secondIndex++;
            }
            else if (first.ParagraphId < second.ParagraphId)
            {
                firstIndex++;
            }
            else
            {
                secondIndex++;
            }
        }
    }

    // ---------- جستجوی تک‌واژه‌ای ----------

    private IEnumerable<SearchResult> SearchSingleTerm(string term)
    {
        foreach (var posting in index.GetPostings(term))
            yield return BuildResult(posting.ParagraphId);
    }

    // ---------- کمکی‌های جستجوی عبارتی ----------

    private List<IReadOnlyList<PostingEntry>> GetPostingsForEachTerm(IReadOnlyList<string> terms)
    {
        var result = new List<IReadOnlyList<PostingEntry>>();
        foreach (var term in terms)
            result.Add(index.GetPostings(term));
        return result;
    }

    private static bool AnyTermHasNoPostings(List<IReadOnlyList<PostingEntry>> postingsPerTerm)
    {
        foreach (var postings in postingsPerTerm)
        {
            if (postings.Count == 0) return true;
        }
        return false;
    }

    /// برای هر واژه، یک دیکشنری از شناسهٔ پاراگراف به Posting می‌سازد تا جستجو سریع باشد
    private static List<Dictionary<int, PostingEntry>> BuildLookupForEachTerm(
        List<IReadOnlyList<PostingEntry>> postingsPerTerm)
    {
        var result = new List<Dictionary<int, PostingEntry>>();
        foreach (var postings in postingsPerTerm)
        {
            var lookup = new Dictionary<int, PostingEntry>();
            foreach (var posting in postings)
                lookup[posting.ParagraphId] = posting;
            result.Add(lookup);
        }
        return result;
    }

    /// پاراگراف‌هایی که در Postingهای واژهٔ اول هستند و در همهٔ واژه‌های دیگر هم وجود دارند
    private static List<int> FindParagraphIdsContainingAllTerms(
        IReadOnlyList<PostingEntry> firstTermPostings,
        List<Dictionary<int, PostingEntry>> lookupPerTerm)
    {
        var result = new List<int>();
        foreach (var posting in firstTermPostings)
        {
            if (ParagraphExistsInAllLookups(posting.ParagraphId, lookupPerTerm))
                result.Add(posting.ParagraphId);
        }
        return result;
    }

    private static bool ParagraphExistsInAllLookups(
        int paragraphId, List<Dictionary<int, PostingEntry>> lookupPerTerm)
    {
        foreach (var lookup in lookupPerTerm)
        {
            if (!lookup.ContainsKey(paragraphId)) return false;
        }
        return true;
    }

    /// موضع‌های هر واژه داخل یک پاراگراف مشخص (به ترتیب واژه‌های عبارت)
    private static List<List<int>> GetPositionsForEachTerm(
        List<Dictionary<int, PostingEntry>> lookupPerTerm, int paragraphId)
    {
        var result = new List<List<int>>();
        foreach (var lookup in lookupPerTerm)
            result.Add(lookup[paragraphId].Positions);
        return result;
    }

    /// آیا واژه‌ها پشت‌سرهم و به ترتیب آمده‌اند؟ (واژهٔ i باید دقیقاً i خانه بعد از واژهٔ اول باشد)
    private static bool HasSequentialPhrase(List<List<int>> positionsPerTerm)
    {
        foreach (var startPosition in positionsPerTerm[0])
        {
            if (PhraseContinuesFrom(startPosition, positionsPerTerm))
                return true;
        }
        return false;
    }

    private static bool PhraseContinuesFrom(int startPosition, List<List<int>> positionsPerTerm)
    {
        for (int termNumber = 1; termNumber < positionsPerTerm.Count; termNumber++)
        {
            int expectedPosition = startPosition + termNumber;
            if (!ContainsPosition(positionsPerTerm[termNumber], expectedPosition))
                return false;
        }
        return true;
    }

    /// لیست موضع‌ها صعودی است، پس جستجوی دودویی کافی است
    private static bool ContainsPosition(List<int> sortedPositions, int position)
        => sortedPositions.BinarySearch(position) >= 0;

    // ---------- کمکی‌های جستجوی مجاورتی ----------

    private static bool HasClosePositions(List<int> firstPositions, List<int> secondPositions, int maxGap)
    {
        int firstIndex = 0;
        int secondIndex = 0;

        while (firstIndex < firstPositions.Count && secondIndex < secondPositions.Count)
        {
            int firstPosition = firstPositions[firstIndex];
            int secondPosition = secondPositions[secondIndex];

            if (Math.Abs(firstPosition - secondPosition) <= maxGap)
                return true;

            if (firstPosition < secondPosition) firstIndex++;
            else secondIndex++;
        }
        return false;
    }

    // ---------- ساخت نتیجه ----------

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