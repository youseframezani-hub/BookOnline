using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace AdminLibraryOnlins.Domain.Users;

public class User
{
    public int Id { get; set; }
    public UserInfo Info { get; set; }
    public UserRole Role { get; set; }
}
public class YousefUser : User
{
    public TicketResolved TicketResolved { get; }
    public StoreCreated StoreCreated { get; }
}

public class StoreCreated
{
}

public class TicketResolved
{
}

public class StoreUser : User
{
    public StoreOwnerd StoreOwnerd { get; }
    public BookUploeded BookUploeded { get; }
    public TicketRegistered TicketRegistered { get; }
}

public class TicketRegistered
{
}

public class StoreOwnerd
{
}

public class PeopleUser : User
{

    BookReaded BookReaded { get; }
    StoreWatched Watched { get; }
}

internal class BookReaded
{
}

public class CustomerUser : PeopleUser
{
    public StorePaid StorePaid { get; }
    public BookBuyed BookBuyed { get; }
    public Subscriptioned Subscriptioned { get; }
}

public class Subscriptioned
{
}

public class BookBuyed
{
}

public class StorePaid
{
}

public class OtherrUser : PeopleUser
{
}

internal class StoreWatched
{
}

public class UserRole
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Permission> Permissions { get; set; }
}

public class Permission
{
}

public class UserInfo
{
}