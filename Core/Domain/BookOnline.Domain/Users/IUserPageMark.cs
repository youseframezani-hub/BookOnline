using BookOnline.Domain.Books;

namespace BookOnline.Domain.Users;

public interface IUserPageMark
{
    PageId PageId { get; set; }
    UserId UserId { get; set; }
}