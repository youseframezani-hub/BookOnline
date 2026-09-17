using System.Text;

namespace BookImporter.Logging;

/// <summary>
/// لاگری سبک و مقصود‌محور: هر پیام هم‌زمان در کنسول و در یک فایل Markdown فارسی نوشته می‌شود.
/// ساختار فایل خروجی: یک تیتر اصلی، سپس برای هر مرحلهٔ اجرا یک بخش (##) با فهرستی از خط‌های
/// اطلاعات/موفقیت/هشدار/خطا، و در پایان یک جدول خلاصهٔ آماری.
/// </summary>
public sealed class MarkdownConsoleLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public string LogFilePath { get; }

    public MarkdownConsoleLogger(string logFilePath, string reportTitle)
    {
        LogFilePath = logFilePath;

        var directory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _writer = new StreamWriter(logFilePath, append: false, new UTF8Encoding(false))
        {
            AutoFlush = true
        };

        _writer.WriteLine($"# {reportTitle}");
        _writer.WriteLine();
        _writer.WriteLine($"- تاریخ اجرا: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
        _writer.WriteLine();
    }

    public void Section(string title)
    {
        lock (_lock)
        {
            Console.WriteLine();
            Console.WriteLine($"== {title} ==");

            _writer.WriteLine($"## {title}");
            _writer.WriteLine();
        }
    }

    public void Info(string message) => Write(LogLevel.Info, message);
    public void Success(string message) => Write(LogLevel.Success, message);
    public void Warning(string message) => Write(LogLevel.Warning, message);
    public void Error(string message) => Write(LogLevel.Error, message);

    public void Table(string title, IReadOnlyList<(string Key, string Value)> rows)
    {
        lock (_lock)
        {
            Console.WriteLine();
            Console.WriteLine($"-- {title} --");
            foreach (var (key, value) in rows)
                Console.WriteLine($"  {key}: {value}");

            _writer.WriteLine();
            _writer.WriteLine($"### {title}");
            _writer.WriteLine();
            _writer.WriteLine("| مورد | مقدار |");
            _writer.WriteLine("|---|---|");
            foreach (var (key, value) in rows)
                _writer.WriteLine($"| {key} | {value} |");
            _writer.WriteLine();
        }
    }

    private void Write(LogLevel level, string message)
    {
        lock (_lock)
        {
            var (consolePrefix, mdPrefix, color) = level switch
            {
                LogLevel.Success => ("[موفق]", "**موفق:**", ConsoleColor.Green),
                LogLevel.Warning => ("[هشدار]", "**هشدار:**", ConsoleColor.Yellow),
                LogLevel.Error => ("[خطا]", "**خطا:**", ConsoleColor.Red),
                _ => ("[اطلاعات]", string.Empty, ConsoleColor.Gray)
            };

            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"{consolePrefix} {message}");
            Console.ForegroundColor = previousColor;

            _writer.WriteLine(string.IsNullOrEmpty(mdPrefix)
                ? $"- {message}"
                : $"- {mdPrefix} {message}");
        }
    }

    public void Dispose()
    {
        _writer.Flush();
        _writer.Dispose();
    }

    private enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error
    }
}
