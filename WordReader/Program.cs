using System.Text;
using System.Text.Encodings.Web;
using WordReader.Services.Models;
using WordReader.Services.WordExtractor;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddScoped<ITextExtractor, InteropTextExtractor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

RunWordExtractor(app);

app.Run();

static void RunWordExtractor(WebApplication app)
{
    using var scoped = app.Services.CreateScope();
    var extractor = scoped.ServiceProvider.GetService<ITextExtractor>();
    var document = extractor.Extract(@"C:\raha_part 02_08-02-1404-mini.docx");

    //foreach (var page in document.Pages)
    //{
    //    Console.WriteLine($"Page: {page.PageNumber}");

    //    foreach (var paragraph in page.Paragraphs)
    //    {
    //        Console.WriteLine($"  Paragraph: {paragraph.ParagraphIndex}");
    //        Console.WriteLine($"  Style: {paragraph.StyleName}");
    //        Console.WriteLine($"  Text: {paragraph.Text}");
    //        Console.WriteLine();
    //    }
    //}
    var ss = string.Join(Environment.NewLine, document.Styles.Values);
    var paragraphs = document.Pages.SelectMany(p => p.Paragraphs).ToList();
    string date = "", ravi = "", fehrest = "", story = "";
    List<IndexItem> index = new List<IndexItem>();
    int rowIndex = 0;
    foreach (var paragraph in paragraphs)
    {
        //فهرست
        //تاریخ
        //داستان
        //راوی

        if (paragraph.StyleName.Trim().Equals("فهرست"))
        {
            fehrest = paragraph.Text;
            date = "";
            ravi = "";
            story = "";
        }

        if (paragraph.StyleName.Trim().Equals("تاریخ"))
            date = paragraph.Text;

        if (paragraph.StyleName.Trim().Equals("داستان"))
            story = paragraph.Text;

        if (paragraph.StyleName.Trim().Equals("راوی"))
            ravi = paragraph.Text;

        if (paragraph.StyleName.Trim().Equals("TOC 1"))
        {
            rowIndex++;
            var text = paragraph.Text;
            //"25 شعبانِ 190-/ مراحل سه‌گانۀ تسلط خُزاعه بر کعبه و حرم تا تصدیِ قُصَیّ بْن کِلاب\t95"
            var s = text.Split(new string[] { "\t", "/" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var title = "";
            if (s.Length > 0) 
             title = s[0];
            var tarikh = "";
            if (s.Length > 1)
                tarikh = s[1];
            var num = -1;
            if (s.Length > 2 && int.TryParse(s[2],out var n))
                num = n;
            index.Add(new IndexItem
            {
                RowNumber = rowIndex,
                BookIndex = num,
                Title = tarikh,
                Date = title
            });
        }


        paragraph.Fehrest = fehrest;
        paragraph.Story = story;
        paragraph.Ravi = ravi;
        paragraph.Date = date;

    }

    document.Index = index;
    document.Title = string.Join(",  ", document.Pages.First().Paragraphs.Select(s => s.Text));
    var str = System.Text.Json.JsonSerializer.Serialize(document, options: new System.Text.Json.JsonSerializerOptions
    {

        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });

    File.WriteAllText(
        "output.txt",
        str,
        new UTF8Encoding(false)); // UTF-8 ???? BOM
}