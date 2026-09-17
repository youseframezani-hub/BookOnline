namespace SearchEngine.Models;

public sealed class AtomicBlockSection
{
    public required string Term { get; init; }   // واژهٔ نرمال‌شده
    public int Position { get; init; }           // شماره واژه در پاراگراف
    public int Offset { get; init; }              // آفست کاراکتری برای هایلایت
    public int ParagraphId { get; init; }         // واحد مبنای مجاورت
}
