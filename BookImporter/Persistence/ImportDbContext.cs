using System.Text.Json;
using BookImporter.Persistence.Entities;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using WordReader.Services.Models;

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
    public DbSet<BookStyleEntity> Styles => Set<BookStyleEntity>();
    public DbSet<StyleParagraphPropertiesEntity> StyleParagraphProperties => Set<StyleParagraphPropertiesEntity>();
    public DbSet<StyleRunPropertiesEntity> StyleRunProperties => Set<StyleRunPropertiesEntity>();
    public DbSet<StyleTablePropertiesEntity> StyleTableProperties => Set<StyleTablePropertiesEntity>();


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
        modelBuilder.Entity<BookStyleEntity>()
            .HasKey(e => e.Name);
        modelBuilder.Entity<BookStyleEntity>()
            .Property(e => e.Type)
            .HasConversion<string>();

        modelBuilder.Entity<StyleParagraphPropertiesEntity>()
            .Property(e => e.Justification)
            .HasConversion<string>();
        modelBuilder.Entity<StyleParagraphPropertiesEntity>()
            .Property(e => e.OutlineLevel)
            .HasConversion<string>();

        modelBuilder.Entity<StyleParagraphPropertiesEntity>()
            .Property(e => e.FrameProperties)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<FramePropertiesModel>(e, JsonConfiguration.Options));
        modelBuilder.Entity<StyleParagraphPropertiesEntity>()
            .Property(e => e.Indentation)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<IndentationModel>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleParagraphPropertiesEntity>()
            .Property(e => e.ParagraphBorders)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<ParagraphBordersModel>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleParagraphPropertiesEntity>()
            .Property(e => e.Shading)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<ShadingModel>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleParagraphPropertiesEntity>()
            .Property(e => e.Spacing)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<SpacingBetweenLinesModel>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleParagraphPropertiesEntity>()
            .Property(e => e.Tabs)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<List<TabStopModel>>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.Border)
            .HasConversion(e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<BorderModel>(e, JsonConfiguration.Options));
        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.EastAsianLayout)
            .HasConversion(e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<EastAsianLayoutModel>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.RunFonts)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<RunFontsModel>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.Languages)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<LanguageModel>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.FitText)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<FitTextModel>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.Color)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<ColorModel>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.Shading)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<ShadingModel>(e, JsonConfiguration.Options));

        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.Underline)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<UnderlineModel>(e, JsonConfiguration.Options));


        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.VerticalTextAlignment)
            .HasConversion<string>();


        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.TextEffect)
            .HasConversion<string>();
        modelBuilder.Entity<StyleRunPropertiesEntity>()
            .Property(e => e.Emphasis)
            .HasConversion<string>();
        modelBuilder.Entity<StyleTablePropertiesEntity>()
            .Property(e => e.Shading)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<ShadingModel>(e, JsonConfiguration.Options)
            );
        modelBuilder.Entity<StyleTablePropertiesEntity>()
            .Property(e => e.TableBorders)
            .HasConversion(
                e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<TableBordersModel>(e, JsonConfiguration.Options)
            );
        modelBuilder.Entity<StyleTablePropertiesEntity>()
            .Property(e => e.TableCellMarginDefault)
            .HasConversion(e => JsonSerializer.Serialize(e, JsonConfiguration.Options),
                e => JsonSerializer.Deserialize<TableCellMarginDefaultModel>(e, JsonConfiguration.Options));
        modelBuilder.Entity<StyleTablePropertiesEntity>()
            .Property(e => e.TableCellSpacing)
            .HasConversion(
                e=>JsonSerializer.Serialize(e,JsonConfiguration.Options),
                e=>JsonSerializer.Deserialize<TableCellSpacingModel>(e,JsonConfiguration.Options)
            );

        modelBuilder.Entity<StyleTablePropertiesEntity>()
            .Property(e => e.TableIndentation)
            .HasConversion(
                e=>JsonSerializer.Serialize(e,JsonConfiguration.Options),
                e=>JsonSerializer.Deserialize<TableIndentationModel>(e,JsonConfiguration.Options)
            );

        modelBuilder.Entity<StyleTablePropertiesEntity>()
            .Property(e => e.TableJustification)
            .HasConversion<string>();

        modelBuilder.Entity<StyleTablePropertiesEntity>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<StyleTablePropertiesEntity>()
            .Property(e => e.Id)
            .ValueGeneratedOnAdd();



    }
}