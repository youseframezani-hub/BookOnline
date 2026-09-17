namespace SearchEngine.Models;

public sealed class SearchResult
{
    public required int ParagraphId { get; init; }
    public required int BookId { get; init; }
    public required int PageNumber { get; init; }
    public required string RawText { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<(int Start, int Length)> Highlights { get; } = new();
}
