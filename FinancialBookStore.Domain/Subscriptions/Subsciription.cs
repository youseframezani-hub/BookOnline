using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialBookStore.Domain.Subscriptions;
public class Order
{
    public int Id { get; set; }
    public int SubsciriptionId { get; set; }
    public UserId UserId { get; set; }
    public Price Price { get; set; }
    public DateTime BuyDate { get; set; }
    public DateTime? ExpireDate { get; set; }
    public OrderStatus Status { get; set; }
}

public record OrderStatus();

public class Subsciription
{
    public int Id { get; set; }
    public StoreId StoreId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Price Price { get; set; }
}
public class BookSubsciription: Subsciription
{
    public BookId BookId { get; set; }
}

public record UserId();

public record BookId();

public class TimeSubsciription: Subsciription
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class Price
{
}

public class StoreId
{
}