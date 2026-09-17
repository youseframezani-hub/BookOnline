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
public class HiLights : IUserPageMark
{
    public UserId UserId { get; set; }
    public PageId PageId { get; set; }
    PragraphSection PragraphSection { get; set; }
    public HiLightsMark Start { get; }
    public HiLightsMark End { get; }
}
public class BookMark(int Id,PageId pageId);
public record HiLightsMark(AtomicBlockSection AtomicBlockSection,int CharacterIndex);
public record UserId();