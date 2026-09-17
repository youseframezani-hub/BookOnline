using System.Diagnostics;
using BookImporter.Logging;
using BookImporter.Mapping;
using BookImporter.Persistence;
using SearchEngine.Analysis;
using SearchEngine.Indexing;
using SearchEngine.Search;
using WordReader.Services.WordExtractor;

const int BookId = 1;

var sourceFilePath = args.Length > 0
    ? args[0]
    : @"C:\raha_part 02_08-02-1404-mini.docx";

var logFilePath = Path.Combine("Logs", $"import-{DateTime.Now:yyyyMMdd-HHmmss}.md");
var dbFilePath = Path.Combine("Data", "books.db");

using var logger = new MarkdownConsoleLogger(logFilePath, "گزارش وارد‌سازی کتاب به دامنه");
var stopwatch = Stopwatch.StartNew();

try
{
    logger.Section("۱. استخراج سند Word");
    logger.Info($"مسیر فایل ورودی: {sourceFilePath}");

    ITextExtractor extractor = new InteropTextExtractor();
    var document = extractor.Extract(sourceFilePath);

    logger.Success($"استخراج با موفقیت انجام شد. تعداد صفحات: {document.Pages.Count}");

    logger.Section("۲. نگاشت به مدل دامنه (IBook / IPage / PragraphSection)");
    var mapper = new DocumentToBookMapper();
    var book = mapper.Map(document, BookId);

    var paragraphCount = book.Pages.Sum(p => p.PragraphSections.Count);
    logger.Success($"نگاشت انجام شد. عنوان کتاب: «{book.Info.Title}»");
    logger.Info($"تعداد پاراگراف‌های نگاشت‌شده: {paragraphCount}");
    logger.Info($"تعداد آیتم‌های فهرست مطالب استخراج‌شده: {book.Index.Items.Count}");

    logger.Section("۳. ذخیره‌سازی در دیتابیس (متن خام، بدون ABP)");
    Directory.CreateDirectory("Data");

    using (var db = new ImportDbContext(dbFilePath))
    {
        db.Database.EnsureCreated();
        var repository = new BookRepository(db);
        var savedBookId = repository.SaveBook(book, sourceFilePath);
        logger.Success($"کتاب با شناسهٔ دیتابیسی {savedBookId} در «{dbFilePath}» ذخیره شد.");
    }

    logger.Section("۴. ساخت ایندکس معکوس موضعی (کاملاً در حافظه)");
    var index = new PositionalInvertedIndex();
    var normalizer = new PersianNormalizer();
    var paragraphId = 0;

    foreach (var page in book.Pages)
        foreach (var paragraph in page.PragraphSections)
            index.AddParagraph(++paragraphId, BookId, page.Id.PageNumber, paragraph.RawText, normalizer);

    logger.Success($"ایندکس‌سازی {index.ParagraphCount} پاراگراف در حافظه به پایان رسید.");

    logger.Section("۵. آزمایش موتور جستجو");
    var searchService = new SearchService(index);
    var titleTerms = normalizer.Tokenize(book.Info.Title)
        .Select(t => t.Term)
        .Distinct()
        .Take(2)
        .ToList();

    if (titleTerms.Count >= 2)
    {
        var phraseResults = searchService.PhraseSearch(titleTerms).ToList();
        logger.Info($"جستجوی عبارتی آزمایشی برای «{string.Join(" ", titleTerms)}»: {phraseResults.Count} نتیجه.");
    }
    else if (titleTerms.Count == 1)
    {
        var postings = index.GetPostings(titleTerms[0]);
        logger.Info($"جستجوی تک‌واژه‌ای آزمایشی برای «{titleTerms[0]}»: در {postings.Count} پاراگراف یافت شد.");
    }
    else
    {
        logger.Warning("عنوان کتاب واژهٔ قابل‌ایندکسی (غیر از حروف‌اضافه) نداشت؛ جستجوی آزمایشی رد شد.");
    }

    stopwatch.Stop();

    logger.Section("خلاصهٔ اجرا");
    logger.Table("آمار نهایی", new (string Key, string Value)[]
    {
        ("تعداد صفحات", document.Pages.Count.ToString()),
        ("تعداد پاراگراف‌های نگاشت‌شده", paragraphCount.ToString()),
        ("تعداد آیتم‌های فهرست مطالب", book.Index.Items.Count.ToString()),
        ("تعداد پاراگراف‌های ایندکس‌شده", index.ParagraphCount.ToString()),
        ("مسیر دیتابیس", Path.GetFullPath(dbFilePath)),
        ("مسیر فایل لاگ", Path.GetFullPath(logFilePath)),
        ("مدت‌زمان کل اجرا", $"{stopwatch.Elapsed.TotalSeconds:F1} ثانیه"),
    });

    logger.Success("فرآیند وارد‌سازی کتاب با موفقیت به پایان رسید.");
}
catch (Exception ex)
{
    stopwatch.Stop();
    logger.Error($"اجرا با خطا متوقف شد: {ex.Message}");
    logger.Info(ex.ToString());
    Environment.ExitCode = 1;
}
