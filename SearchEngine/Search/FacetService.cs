using SearchEngine.Models;

namespace SearchEngine.Search;

public sealed class FacetService
{
    public IReadOnlyDictionary<int, FacetGroup> GroupByBook(
        IEnumerable<SearchResult> results)
    {
        var groups = new Dictionary<int, FacetGroup>();

        foreach (var r in results)
        {
            if (!groups.TryGetValue(r.BookId, out var g))
                groups[r.BookId] = g = new FacetGroup { BookId = r.BookId };
            g.HitCount++;
            g.PageNumbers.Add(r.PageNumber);   // HashSet → شماره صفحهٔ یکتا
        }
        return groups;
    }
}

public sealed class FacetGroup
{
    public required int BookId { get; init; }
    public int HitCount { get; set; }
    public HashSet<int> PageNumbers { get; } = new();
}
