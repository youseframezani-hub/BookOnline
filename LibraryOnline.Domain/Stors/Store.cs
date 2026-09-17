using BookOnline.Domain.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOnline.Domain.Stors;

public class Store
{
    public StoreId StoreId { get;  }
    public StoreInfo StoreInfo { get; }
    public LandingPageInfo LandingPageInfo { get; }
    public List<CategoryBooks> CategoryBooks { get; }
    public Category Category { get; }
}

public record CategoryBooks(CategoryId CategoryId,BookId BookId);

public class Category
{
    public CategoryId Id { get; set; }
    public bool IsRoot => Parent == null;
    public bool IsChild =>  Childs?.Any() != true;
    public Category? Parent { get; set; }
    public string Title { get; set; }
    public List<Category>? Childs { get; set; }
}

public record CategoryId(List<int> Id);

public class LandingPageInfo
{
    public int Id { get;}
    public PageInfo PageInfo { get; }
    public List<OtherPage> OtherPages { get; }
}

public class OtherPage
{
    public string Title { get; }
    public PageInfo PageInfo { get; }
}

public class PageInfo
{
}

public class StoreInfo
{
}

public struct StoreId();
