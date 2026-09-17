namespace SearchEngine.Models;

public sealed class PostingEntry
{
    public required int ParagraphId { get; init; }
    public required int BookId { get; init; }     // Facet
    public required int PageNumber { get; init; } // Facet
    public List<int> Positions { get; } = new();   // باید صعودی بماند
    public List<int> Offsets { get; } = new();     // موازی با Positions

    public void AddPosition(int position, int offset)
    {
        Positions.Add(position);
        Offsets.Add(offset);
    }
}
