using BookOnline.Domain.Books;

namespace BookOnline.Domain.Users;

public class User
{
    public UserId UserId { get; }
    public UserInfo Info { get; }
    public List<BookMark>? BookMarks { get; }
    public List<HiLights>? HiLights { get; }
    public List<Note>? Nots { get; }
    public List<IUserPageMark>? UserPageMarks => HiLights?.Select(s => (IUserPageMark)s)?.ToList();
}
public record UserInfo();
public class Note : IUserPageMark
{

    public UserId UserId { get; set; }
    public PageId PageId { get; set; }
    public NoteContent Content { get; set; }
    public record NoteContent();
}
public sealed class HiLights : IUserPageMark
{
    public required UserId UserId { get; set; }
    public required PageId PageId { get; set; }
    public required HiLightsMark Start { get; init; }
    public required HiLightsMark End { get; init; }
}
public class BookMark(int Id,PageId pageId);

/// <summary>
/// نقطهٔ شروع/پایان یک هایلایت، به‌صورت آفست کاراکتری در متن خام پاراگراف
/// (<see cref="PragraphSection.RawText"/>) — هم‌راستا با مفهوم Offset در PostingEntry
/// پروژهٔ SearchEngine. پیشتر به AtomicBlockSection (که حذف شده) وابسته بود.
/// </summary>
public record HiLightsMark(int CharacterOffset);
public record UserId();
