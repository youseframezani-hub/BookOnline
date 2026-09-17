using BookImporter.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookImporter.Persistence;

/// <summary>
/// DbContext کاملاً خالص (Vanilla EF Core)، بدون هیچ وابستگی به ABP یا هر لایهٔ Unit-of-Work/Repository
/// اضافه‌ای. فقط برای ذخیرهٔ متن خام کتاب/صفحه/پاراگراف در یک فایل SQLite ساده.
/// </summary>
public sealed class ImportDbContext : DbContext
{
    private readonly string _sqliteFilePath;

    public ImportDbContext(string sqliteFilePath)
    {
        _sqliteFilePath = sqliteFilePath;
    }

    public DbSet<BookEntity> Books => Set<BookEntity>();
    public DbSet<PageEntity> Pages => Set<PageEntity>();
    public DbSet<ParagraphEntity> Paragraphs => Set<ParagraphEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite($"Data Source={_sqliteFilePath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookEntity>()
            .HasMany(b => b.Pages)
            .WithOne(p => p.Book)
            .HasForeignKey(p => p.BookId);

        modelBuilder.Entity<PageEntity>()
            .HasMany(p => p.Paragraphs)
            .WithOne(a => a.Page)
            .HasForeignKey(a => a.PageId);
    }
}
