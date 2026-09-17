using Microsoft.Office.Interop.Word;
using WordReader.Services.Models;
using WordApplication = Microsoft.Office.Interop.Word.Application;
using WordDocument = Microsoft.Office.Interop.Word.Document;
using WordParagraph = Microsoft.Office.Interop.Word.Paragraph;
using WordRange = Microsoft.Office.Interop.Word.Range;
using WordStyle = Microsoft.Office.Interop.Word.Style;
using WordInlineShape = Microsoft.Office.Interop.Word.InlineShape;
using WordShape = Microsoft.Office.Interop.Word.Shape;
using WordTable = Microsoft.Office.Interop.Word.Table;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using WordSection = Microsoft.Office.Interop.Word.Section;
using WordHeaderFooter = Microsoft.Office.Interop.Word.HeaderFooter;
using Field = Microsoft.Office.Interop.Word.Field;
using WordFootnote = Microsoft.Office.Interop.Word.Footnote;
using WordFootnotes = Microsoft.Office.Interop.Word.Footnotes;


namespace WordReader.Services.WordExtractor;

public interface ITextExtractor
{
    DocumentContent Extract(string filePath);
}

public sealed class InteropTextExtractor : ITextExtractor
{
    private readonly bool _includeEmptyParagraphFragments;

    public InteropTextExtractor(bool includeEmptyParagraphFragments = false)
    {
        _includeEmptyParagraphFragments = includeEmptyParagraphFragments;
    }

    public DocumentContent Extract(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Word file not found.", filePath);

        WordApplication? app = null;
        WordDocument? doc = null;

        try
        {
            app = new WordApplication
            {
                Visible = false,
                ScreenUpdating = false,
                DisplayAlerts = WdAlertLevel.wdAlertsNone
            };

            object readOnly = true;
            object isVisible = false;
            object confirmConversions = false;
            object addToRecentFiles = false;

            doc = app.Documents.Open(
                FileName: filePath,
                ConfirmConversions: ref confirmConversions,
                ReadOnly: ref readOnly,
                AddToRecentFiles: ref addToRecentFiles,
                Visible: ref isVisible);

            doc.Activate();
            doc.Repaginate();

            var result = new DocumentContent
            {
                FilePath = filePath
            };

            var totalPages = GetTotalPages(doc);
            if (totalPages <= 0)
                return result;

            var paragraphInfos = ReadParagraphInfos(doc, result.Styles);
            var tableInfos = ReadTableInfos(doc);
            var imageInfos = ReadImageInfos(doc, filePath);

            for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                WordRange? pageRange = null;

                try
                {
                    pageRange = GetPageRange(doc, pageNumber, totalPages);

                    var page = new PageContent
                    {
                        PageNumber = pageNumber,
                        Text = CleanPageText(pageRange.Text)
                    };

                    AddParagraphFragmentsForPage(page, pageRange, paragraphInfos);
                    AddTablesForPage(page, pageRange, tableInfos);
                    AddImagesForPage(page, pageRange, imageInfos);
                    AddHeaderAndFooterForPage(doc, page, pageRange);
                    AddFootnotesForPage(page, doc, pageNumber);

                    page.RebuildHtml();

                    result.Pages.Add(page);
                }
                finally
                {
                    ReleaseComObject(pageRange);
                }
            }

            return result;
        }
        finally
        {
            if (doc != null)
            {
                try
                {
                    object saveChanges = WdSaveOptions.wdDoNotSaveChanges;
                    doc.Close(ref saveChanges);
                }
                catch
                {
                }

                ReleaseComObject(doc);
            }

            if (app != null)
            {
                try
                {
                    app.Quit(WdSaveOptions.wdDoNotSaveChanges);
                }
                catch
                {
                }

                ReleaseComObject(app);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
    private void AddFootnotesForPage(PageContent page, WordDocument doc, int pageNumber)
    {
        WordFootnotes? footnotes = null;

        try
        {
            footnotes = doc.Footnotes;

            for (var i = 1; i <= footnotes.Count; i++)
            {
                WordFootnote? footnote = null;
                WordRange? referenceRange = null;
                WordRange? footnoteRange = null;

                try
                {
                    footnote = footnotes[i];
                    referenceRange = footnote.Reference;

                    var footnotePageNumber =
                        (int)referenceRange.Information[WdInformation.wdActiveEndPageNumber];

                    if (footnotePageNumber != pageNumber)
                        continue;

                    footnoteRange = footnote.Range;

                    var referenceContext = BuildFootnoteReferenceContext(footnote, referenceRange);

                    AddParagraphsFromStoryRange(
                        page,
                        footnoteRange,
                        page.Footnotes,
                        isHeader: false,
                        isFooter: false,
                        isFootnote: true,
                        footnoteReferenceContext: referenceContext);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        ex.Message +
                        $". Could not extract footnote {i} for page {pageNumber}");
                }
                finally
                {
                    ReleaseComObject(footnoteRange);
                    ReleaseComObject(referenceRange);
                    ReleaseComObject(footnote);
                }
            }
        }
        finally
        {
            ReleaseComObject(footnotes);
        }

    }
    private FootnoteReferenceContext BuildFootnoteReferenceContext(
    WordFootnote footnote,
    WordRange referenceRange)
    {
        var context = new FootnoteReferenceContext();

        try
        {
            context.ReferenceNumber = SafeGetFootnoteReferenceNumber(footnote);
            ExtractReferenceWords(referenceRange, context);
        }
        catch
        {
        }

        return context;
        string? SafeGetFootnoteReferenceNumber(WordFootnote footnote)
        {
            try
            {
                var value = footnote.Index;
                return value > 0 ? value.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

    }
    private void ExtractReferenceWords(
    WordRange referenceRange,
    FootnoteReferenceContext context)
    {
        WordRange? hostWordRange = null;
        WordRange? preceding1Range = null;
        WordRange? preceding2Range = null;

        try
        {
            hostWordRange = GetWordBeforeReference(referenceRange, 1);
            preceding1Range = GetWordBeforeReference(referenceRange, 2);
            preceding2Range = GetWordBeforeReference(referenceRange, 3);

            context.ReferenceWord = CleanWord(hostWordRange?.Text);
            context.PrecedingWord1 = CleanWord(preceding1Range?.Text);
            context.PrecedingWord2 = CleanWord(preceding2Range?.Text);
        }
        finally
        {
            ReleaseComObject(hostWordRange);
            ReleaseComObject(preceding1Range);
            ReleaseComObject(preceding2Range);
        }
        string? CleanWord(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var cleaned = text
                .Replace("\r", " ")
                .Replace("\a", " ")
                .Trim();

            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

            if (string.IsNullOrWhiteSpace(cleaned))
                return null;

            return cleaned;
        }

    }
    private WordRange? GetWordBeforeReference(WordRange referenceRange, int wordOffsetFromReference)
    {
        WordRange? probeRange = null;

        try
        {
            probeRange = referenceRange.Duplicate;
            probeRange.Collapse(WdCollapseDirection.wdCollapseStart);

            var movedStart = probeRange.MoveStart(WdUnits.wdWord, -wordOffsetFromReference);
            var movedEnd = probeRange.MoveEnd(WdUnits.wdWord, -(wordOffsetFromReference - 1));

            if (movedStart == 0 && movedEnd == 0)
                return null;

            return probeRange.Duplicate;
        }
        catch
        {
            ReleaseComObject(probeRange);
            return null;
        }
    }

    private void AddParagraphsFromStoryRange(
        PageContent page,
        WordRange range,
        List<ParagraphSpan> target,
        bool isHeader,
        bool isFooter,
        bool isFootnote,
        FootnoteReferenceContext? footnoteReferenceContext = null)
    {
        WordParagraph? paragraph = null;
        WordRange? paragraphRange = null;

        try
        {
            var paragraphs = range.Paragraphs;

            for (var i = 1; i <= paragraphs.Count; i++)
            {
                try
                {
                    paragraph = paragraphs[i];
                    paragraphRange = paragraph.Range;

                    var text = CleanParagraphFragmentText(paragraphRange.Text);

                    if (!_includeEmptyParagraphFragments && string.IsNullOrWhiteSpace(text))
                        continue;

                    var styleName = string.Empty;

                    try
                    {
                        dynamic style = paragraph.get_Style();
                        styleName = style?.NameLocal ?? string.Empty;
                        ReleaseComObject(style);
                    }
                    catch
                    {
                        styleName = string.Empty;
                    }

                    target.Add(new ParagraphSpan
                    {
                        PageNumber = page.PageNumber,
                        ParagraphIndex = i,
                        FragmentIndex = 1,
                        Text = text,
                        StyleName = styleName,
                        IsSplitFromMultiPageParagraph = false,
                        IsFootnote = isFootnote,
                        FootnoteReferenceContext = isFootnote
                            ? CloneFootnoteReferenceContext(footnoteReferenceContext)
                            : null
                    });
                }
                finally
                {
                    ReleaseComObject(paragraphRange);
                    ReleaseComObject(paragraph);

                    paragraphRange = null;
                    paragraph = null;
                }
            }
        }
        finally
        {
        }
        FootnoteReferenceContext? CloneFootnoteReferenceContext(FootnoteReferenceContext? source)
        {
            if (source == null)
                return null;

            return new FootnoteReferenceContext
            {
                ReferenceNumber = source.ReferenceNumber,
                ReferenceWord = source.ReferenceWord,
                PrecedingWord1 = source.PrecedingWord1,
                PrecedingWord2 = source.PrecedingWord2
            };
        }

    }

    private void AddHeaderAndFooterForPage(
    WordDocument doc,
    PageContent page,
    WordRange pageRange)
    {
        WordSection? section = null;
        WordHeaderFooter? header = null;
        WordHeaderFooter? footer = null;

        try
        {
            var sectionNumberObject =
                pageRange.Information[WdInformation.wdActiveEndSectionNumber];

            var sectionNumber = sectionNumberObject is int value && value > 0
                ? value
                : 0;

            if (sectionNumber <= 0 || sectionNumber > doc.Sections.Count)
                return;

            section = doc.Sections[sectionNumber];

            header = GetHeaderForPage(section, page.PageNumber);
            if (header != null && header.Exists)
            {
                AddHeaderFooterSpan(
                    page.Header,
                    header,
                    page.PageNumber,
                    sectionNumber,
                    "Header");
            }

            footer = GetFooterForPage(section, page.PageNumber);
            if (footer != null && footer.Exists)
            {
                AddHeaderFooterSpan(
                    page.Footer,
                    footer,
                    page.PageNumber,
                    sectionNumber,
                    "Footer");
            }
        }
        finally
        {
            ReleaseComObject(header);
            ReleaseComObject(footer);
            ReleaseComObject(section);
        }
    }
    private void AddHeaderFooterSpan(
    List<ParagraphSpan> target,
    WordHeaderFooter headerFooter,
    int pageNumber,
    int sectionNumber,
    string fallbackStyleName)
    {
        var text = ExtractVisibleTextFromHeaderFooter(headerFooter);

        if (!_includeEmptyParagraphFragments && string.IsNullOrWhiteSpace(text))
            return;

        target.Add(new ParagraphSpan
        {
            PageNumber = pageNumber,
            ParagraphIndex = sectionNumber,
            FragmentIndex = 1,
            Text = text,
            StyleName = fallbackStyleName,
            IsSplitFromMultiPageParagraph = false
        });
    }
    private string ExtractVisibleTextFromHeaderFooter(WordHeaderFooter headerFooter)
    {
        WordRange? range = null;

        try
        {
            range = headerFooter.Range;
            if (range == null)
                return string.Empty;

            // 1) متن مستقیم کل StoryRange
            var directText = CleanHeaderFooterText(range.Text);
            if (!string.IsNullOrWhiteSpace(directText))
                return directText;

            // 2) اگر footer شامل فیلدهایی مثل PAGE / NUMPAGES باشد
            var fieldText = ExtractTextFromFields(range);
            if (!string.IsNullOrWhiteSpace(fieldText))
                return fieldText;

            // 3) اگر متن داخل Shape یا TextBox باشد
            var shapeText = ExtractTextFromShapes(range);
            if (!string.IsNullOrWhiteSpace(shapeText))
                return shapeText;

            return string.Empty;
        }
        finally
        {
            ReleaseComObject(range);
        }
    }
    private string ExtractTextFromFields(WordRange range)
    {
        var parts = new List<string>();
        Fields? fields = null;

        try
        {
            fields = range.Fields;

            if (fields == null || fields.Count == 0)
                return string.Empty;

            for (var i = 1; i <= fields.Count; i++)
            {
                Field? field = null;
                WordRange? resultRange = null;

                try
                {
                    field = fields[i];
                    resultRange = field.Result;

                    var text = CleanHeaderFooterText(resultRange?.Text);
                    if (!string.IsNullOrWhiteSpace(text))
                        parts.Add(text);
                }
                catch
                {
                }
                finally
                {
                    ReleaseComObject(resultRange);
                    ReleaseComObject(field);
                }
            }

            return string.Join(" ", parts).Trim();
        }
        finally
        {
            ReleaseComObject(fields);
        }
    }
    private string ExtractTextFromShapes(WordRange range)
    {
        var parts = new List<string>();
        ShapeRange? shapeRange = null;

        try
        {
            try
            {
                shapeRange = range.ShapeRange;
            }
            catch
            {
                return string.Empty;
            }

            if (shapeRange == null || shapeRange.Count == 0)
                return string.Empty;

            for (var i = 1; i <= shapeRange.Count; i++)
            {
                WordShape? shape = null;
                TextFrame? textFrame = null;
                WordRange? textRange = null;

                try
                {
                    shape = shapeRange[i];
                    textFrame = shape.TextFrame;

                    if (textFrame == null || textFrame.HasText == 0)
                        continue;

                    textRange = textFrame.TextRange;

                    var text = CleanHeaderFooterText(textRange?.Text);
                    if (!string.IsNullOrWhiteSpace(text))
                        parts.Add(text);
                }
                catch
                {
                }
                finally
                {
                    ReleaseComObject(textRange);
                    ReleaseComObject(textFrame);
                    ReleaseComObject(shape);
                }
            }

            return string.Join(" ", parts).Trim();
        }
        finally
        {
            ReleaseComObject(shapeRange);
        }
    }
    private static string CleanHeaderFooterText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var cleaned = text
            .Replace("\r", " ")
            .Replace("\a", " ")
            .Replace("\v", " ")
            .Replace("\f", " ");

        return System.Text.RegularExpressions.Regex
            .Replace(cleaned, @"\s+", " ")
            .Trim();
    }


    //private void AddHeaderAndFooterForPage(
    //WordDocument doc,
    //PageContent page,
    //WordRange pageRange)
    //{
    //    WordSection? section = null;
    //    WordHeaderFooter? header = null;
    //    WordHeaderFooter? footer = null;

    //    try
    //    {
    //        var sectionNumberObject =
    //            pageRange.Information[WdInformation.wdActiveEndSectionNumber];

    //        var sectionNumber = sectionNumberObject is int value && value > 0
    //            ? value
    //            : 0;

    //        if (sectionNumber <= 0 || sectionNumber > doc.Sections.Count)
    //            return;

    //        section = doc.Sections[sectionNumber];

    //        header = GetHeaderForPage(section, page.PageNumber);
    //        footer = GetFooterForPage(section, page.PageNumber);

    //        if (header != null && header.Exists)
    //        {
    //            AddParagraphSpansFromRange(
    //                page.Header,
    //                header.Range,
    //                page.PageNumber,
    //                sectionNumber,
    //                isHeader: true);
    //        }

    //        if (footer != null && footer.Exists)
    //        {
    //            AddParagraphSpansFromRange(
    //                page.Footer,
    //                footer.Range,
    //                page.PageNumber,
    //                sectionNumber,
    //                isHeader: false);
    //        }
    //    }
    //    finally
    //    {
    //        ReleaseComObject(header);
    //        ReleaseComObject(footer);
    //        ReleaseComObject(section);
    //    }
    //}
    private static WordHeaderFooter? GetHeaderForPage(
    WordSection section,
    int pageNumber)
    {
        try
        {
            if (section.PageSetup.DifferentFirstPageHeaderFooter != 0 && pageNumber == 1)
            {
                var first = section.Headers[WdHeaderFooterIndex.wdHeaderFooterFirstPage];
                if (first.Exists)
                    return first;

                ReleaseComObject(first);
            }

            if (section.PageSetup.OddAndEvenPagesHeaderFooter != 0)
            {
                var evenOrPrimary = pageNumber % 2 == 0
                    ? WdHeaderFooterIndex.wdHeaderFooterEvenPages
                    : WdHeaderFooterIndex.wdHeaderFooterPrimary;

                var header = section.Headers[evenOrPrimary];
                if (header.Exists)
                    return header;

                ReleaseComObject(header);
            }

            var primary = section.Headers[WdHeaderFooterIndex.wdHeaderFooterPrimary];
            if (primary.Exists)
                return primary;

            ReleaseComObject(primary);
            return null;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
    private static WordHeaderFooter? GetFooterForPage(
    WordSection section,
    int pageNumber)
    {
        try
        {
            if (section.PageSetup.DifferentFirstPageHeaderFooter != 0 && pageNumber == 1)
            {
                var first = section.Footers[WdHeaderFooterIndex.wdHeaderFooterFirstPage];
                if (first.Exists)
                    return first;

                ReleaseComObject(first);
            }

            if (section.PageSetup.OddAndEvenPagesHeaderFooter != 0)
            {
                var evenOrPrimary = pageNumber % 2 == 0
                    ? WdHeaderFooterIndex.wdHeaderFooterEvenPages
                    : WdHeaderFooterIndex.wdHeaderFooterPrimary;

                var footer = section.Footers[evenOrPrimary];
                if (footer.Exists)
                    return footer;

                ReleaseComObject(footer);
            }

            var primary = section.Footers[WdHeaderFooterIndex.wdHeaderFooterPrimary];
            if (primary.Exists)
                return primary;

            ReleaseComObject(primary);
            return null;
        }
        catch
        {
            return null;
        }
    }
    private void AddParagraphSpansFromRange(
    List<ParagraphSpan> target,
    WordRange sourceRange,
    int pageNumber,
    int sectionNumber,
    bool isHeader)
    {
        if (sourceRange == null)
            return;

        var paragraphIndex = 0;

        foreach (WordParagraph paragraph in sourceRange.Paragraphs)
        {
            WordRange? paragraphRange = null;

            try
            {
                paragraphIndex++;
                paragraphRange = paragraph.Range;

                var text = CleanParagraphFragmentText(paragraphRange.Text);

                if (!_includeEmptyParagraphFragments && string.IsNullOrWhiteSpace(text))
                    continue;

                var styleName = GetStyleName(paragraph);

                target.Add(new ParagraphSpan
                {
                    PageNumber = pageNumber,
                    ParagraphIndex = paragraphIndex,
                    FragmentIndex = 1,
                    Text = text,
                    StyleName = styleName,
                    IsSplitFromMultiPageParagraph = false
                });
            }
            finally
            {
                ReleaseComObject(paragraphRange);
                ReleaseComObject(paragraph);
            }
        }
    }

    private void AddParagraphFragmentsForPage(
        PageContent page,
        WordRange pageRange,
        IReadOnlyList<ParagraphInfo> paragraphInfos)
    {
        var pageStart = pageRange.Start;
        var pageEnd = pageRange.End;

        var candidates = paragraphInfos
            .Where(p => RangesIntersect(p.Start, p.End, pageStart, pageEnd))
            .ToList();

        foreach (var paragraph in candidates)
        {
            var fragmentStart = Math.Max(paragraph.Start, pageStart);
            var fragmentEnd = Math.Min(paragraph.End, pageEnd);

            if (fragmentEnd <= fragmentStart)
                continue;

            WordRange? fragmentRange = null;

            try
            {
                fragmentRange = pageRange.Document.Range(fragmentStart, fragmentEnd);

                var text = CleanParagraphFragmentText(fragmentRange.Text);

                if (!_includeEmptyParagraphFragments && string.IsNullOrWhiteSpace(text))
                    continue;

                var isSplit =
                    paragraph.Start < pageStart ||
                    paragraph.End > pageEnd;

                var fragmentIndex = CalculateFragmentIndex(
                    paragraph,
                    page.PageNumber,
                    pageRange.Document);

                page.Paragraphs.Add(new ParagraphSpan
                {
                    PageNumber = page.PageNumber,
                    ParagraphIndex = paragraph.ParagraphIndex,
                    FragmentIndex = fragmentIndex,
                    Text = text,
                    StyleName = paragraph.StyleName,
                    IsSplitFromMultiPageParagraph = isSplit
                });
            }
            finally
            {
                ReleaseComObject(fragmentRange);
            }
        }
    }
    private static void AddTablesForPage(
        PageContent page,
        WordRange pageRange,
        IReadOnlyList<TableInfo> tableInfos)
    {
        var pageStart = pageRange.Start;
        var pageEnd = pageRange.End;

        foreach (var tableInfo in tableInfos)
        {
            if (!RangesIntersect(tableInfo.Start, tableInfo.End, pageStart, pageEnd))
                continue;

            var rowsOnPage = tableInfo.Rows
                .Where(row => RangesIntersect(row.Start, row.End, pageStart, pageEnd))
                .OrderBy(row => row.RowIndex)
                .ToList();

            if (rowsOnPage.Count == 0)
                continue;

            var tableContent = new TableContent
            {
                PageNumber = page.PageNumber,
                TableIndex = tableInfo.TableIndex,

                // اگر این propertyها را در مدل اضافه کرده باشی:
                IsSplitFromMultiPageTable = IsTableSplitOnThisPage(tableInfo, pageStart, pageEnd),
                FirstRowIndexOnPage = rowsOnPage.First().RowIndex,
                LastRowIndexOnPage = rowsOnPage.Last().RowIndex
            };

            foreach (var rowInfo in rowsOnPage)
            {
                tableContent.Rows.Add(CloneTableRowContent(rowInfo.Content));
            }

            page.Tables.Add(tableContent);
        }
    }
    private static TableRowContent CloneTableRowContent(TableRowContent source)
    {
        var row = new TableRowContent
        {
            RowIndex = source.RowIndex
        };

        foreach (var cell in source.Cells)
        {
            row.Cells.Add(new TableCellContent
            {
                RowIndex = cell.RowIndex,
                ColumnIndex = cell.ColumnIndex,
                Text = cell.Text
            });
        }

        return row;
    }
    private static bool IsTableSplitOnThisPage(
    TableInfo tableInfo,
    int pageStart,
    int pageEnd)
    {
        return tableInfo.Start < pageStart || tableInfo.End > pageEnd;
    }

    private static void AddImagesForPage(
        PageContent page,
        WordRange pageRange,
        IReadOnlyList<ImageInfo> imageInfos)
    {
        var pageStart = pageRange.Start;
        var pageEnd = pageRange.End;

        foreach (var imageInfo in imageInfos)
        {
            if (!RangesIntersect(imageInfo.Start, imageInfo.End, pageStart, pageEnd))
                continue;

            page.Images.Add(new ImageContent
            {
                PageNumber = page.PageNumber,
                ImageIndex = imageInfo.ImageIndex,
                ContentType = imageInfo.ContentType,
                Bytes = imageInfo.Bytes
            });
        }
    }

    private static int GetTotalPages(WordDocument doc)
    {
        try
        {
            doc.Repaginate();
            return doc.ComputeStatistics(WdStatistic.wdStatisticPages);
        }
        catch
        {
            return 0;
        }
    }

    private static List<ParagraphInfo> ReadParagraphInfos(
        WordDocument doc,
        Dictionary<string, string> styles)
    {
        var result = new List<ParagraphInfo>();
        var paragraphIndex = 0;

        foreach (WordParagraph paragraph in doc.Paragraphs)
        {
            WordRange? range = null;

            try
            {
                paragraphIndex++;
                range = paragraph.Range;

                var styleName = GetStyleName(paragraph);

                if (!string.IsNullOrWhiteSpace(styleName) &&
                    !styles.ContainsKey(styleName))
                {
                    styles[styleName] = styleName;
                }

                result.Add(new ParagraphInfo
                {
                    ParagraphIndex = paragraphIndex,
                    Start = range.Start,
                    End = range.End,
                    StyleName = styleName
                });
            }
            finally
            {
                ReleaseComObject(range);
                ReleaseComObject(paragraph);
            }
        }

        return result;
    }
    private static List<TableInfo> ReadTableInfos(WordDocument doc)
    {
        var result = new List<TableInfo>();
        var tableIndex = 0;

        foreach (WordTable table in doc.Tables)
        {
            WordRange? tableRange = null;

            try
            {
                tableIndex++;
                tableRange = table.Range;

                var tableInfo = new TableInfo
                {
                    TableIndex = tableIndex,
                    Start = tableRange.Start,
                    End = tableRange.End
                };

                var rowCount = table.Rows.Count;

                for (var rowIndex = 1; rowIndex <= rowCount; rowIndex++)
                {
                    Row? row = null;
                    WordRange? rowRange = null;

                    try
                    {
                        row = table.Rows[rowIndex];
                        rowRange = row.Range;

                        var rowInfo = new TableRowInfo
                        {
                            RowIndex = rowIndex,
                            Start = rowRange.Start,
                            End = rowRange.End,
                            Content = new TableRowContent
                            {
                                RowIndex = rowIndex
                            }
                        };

                        ReadCellsForRow(row, rowInfo.Content);

                        tableInfo.Rows.Add(rowInfo);
                    }
                    catch
                    {
                        // بعضی جدول‌های Word با merged cells یا ساختار نامنظم
                        // ممکن است برای table.Rows[index] خطا بدهند.
                        // در این حالت آن row را رد می‌کنیم.
                    }
                    finally
                    {
                        ReleaseComObject(rowRange);
                        ReleaseComObject(row);
                    }
                }

                result.Add(tableInfo);
            }
            finally
            {
                ReleaseComObject(tableRange);
                ReleaseComObject(table);
            }
        }

        return result;
    }

    private static void ReadCellsForRow(Row row, TableRowContent rowContent)
    {
        var cellIndex = 0;

        foreach (Cell cell in row.Cells)
        {
            WordRange? cellRange = null;

            try
            {
                cellIndex++;

                cellRange = cell.Range;

                rowContent.Cells.Add(new TableCellContent
                {
                    RowIndex = rowContent.RowIndex,
                    ColumnIndex = cellIndex,
                    Text = CleanCellText(cellRange.Text)
                });
            }
            catch
            {
                rowContent.Cells.Add(new TableCellContent
                {
                    RowIndex = rowContent.RowIndex,
                    ColumnIndex = cellIndex,
                    Text = string.Empty
                });
            }
            finally
            {
                ReleaseComObject(cellRange);
                ReleaseComObject(cell);
            }
        }
    }
    private static List<ImageInfo> ReadImageInfos(WordDocument doc, string filePath)
    {
        var result = new List<ImageInfo>();

        var openXmlImages = ReadOpenXmlImages(filePath);

        var imageIndex = 0;

        foreach (WordInlineShape inlineShape in doc.InlineShapes)
        {
            WordRange range = null;

            try
            {
                imageIndex++;
                range = inlineShape.Range;

                var openXmlImage = GetOpenXmlImageByIndexOrDefault(openXmlImages, imageIndex);

                result.Add(new ImageInfo
                {
                    ImageIndex = imageIndex,
                    Start = range.Start,
                    End = range.End,
                    ContentType = openXmlImage != null
                        ? openXmlImage.ContentType
                        : "application/octet-stream",
                    Bytes = openXmlImage != null
                        ? openXmlImage.Bytes
                        : Array.Empty<byte>()
                });
            }
            finally
            {
                ReleaseComObject(range);
                ReleaseComObject(inlineShape);
            }
        }
        foreach (object shapeObject in doc.Shapes)
        {
            WordRange anchorRange = null;

            try
            {
                var shapeType = GetComIntProperty(shapeObject, "Type");

                // 13 = msoPicture
                // 11 = msoLinkedPicture
                if (shapeType != 13 && shapeType != 11)
                {
                    continue;
                }

                imageIndex++;

                anchorRange = GetComProperty<WordRange>(shapeObject, "Anchor");

                var openXmlImage = GetOpenXmlImageByIndexOrDefault(openXmlImages, imageIndex);

                result.Add(new ImageInfo
                {
                    ImageIndex = imageIndex,
                    Start = anchorRange.Start,
                    End = anchorRange.End,
                    ContentType = openXmlImage != null
                        ? openXmlImage.ContentType
                        : "application/octet-stream",
                    Bytes = openXmlImage != null
                        ? openXmlImage.Bytes
                        : Array.Empty<byte>()
                });
            }
            finally
            {
                ReleaseComObject(anchorRange);
                ReleaseComObject(shapeObject);
            }
        }


        return result;
    }

    private static List<OpenXmlImageInfo> ReadOpenXmlImages(string filePath)
    {
        var result = new List<OpenXmlImageInfo>();

        using var wordDocument = WordprocessingDocument.Open(filePath, false);

        var mainPart = wordDocument.MainDocumentPart;
        if (mainPart?.Document == null)
            return result;

        var imageIndex = 0;

        var blips = mainPart.Document
            .Descendants<Blip>()
            .ToList();

        foreach (var blip in blips)
        {
            var relationshipId = blip.Embed?.Value;

            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                relationshipId = blip.Link?.Value;
            }

            if (string.IsNullOrWhiteSpace(relationshipId))
                continue;

            ImagePart? imagePart = null;

            try
            {
                imagePart = mainPart.GetPartById(relationshipId) as ImagePart;
            }
            catch
            {
                imagePart = null;
            }

            if (imagePart == null)
                continue;

            imageIndex++;

            using var stream = imagePart.GetStream();
            using var memoryStream = new MemoryStream();

            stream.CopyTo(memoryStream);

            result.Add(new OpenXmlImageInfo
            {
                ImageIndex = imageIndex,
                RelationshipId = relationshipId,
                ContentType = imagePart.ContentType,
                Bytes = memoryStream.ToArray()
            });
        }

        return result;
    }
    private static OpenXmlImageInfo GetOpenXmlImageByIndexOrDefault(
        IReadOnlyList<OpenXmlImageInfo> images,
        int imageIndex)
    {
        return images.FirstOrDefault(x => x.ImageIndex == imageIndex);
    }
    private static int GetComIntProperty(object comObject, string propertyName)
    {
        var value = comObject
            .GetType()
            .InvokeMember(
                propertyName,
                System.Reflection.BindingFlags.GetProperty,
                null,
                comObject,
                null);

        return Convert.ToInt32(value);
    }

    private static T GetComProperty<T>(object comObject, string propertyName)
    {
        var value = comObject
            .GetType()
            .InvokeMember(
                propertyName,
                System.Reflection.BindingFlags.GetProperty,
                null,
                comObject,
                null);

        return (T)value;
    }


    private static WordRange GetPageRange(
        WordDocument doc,
        int pageNumber,
        int totalPages)
    {
        WordRange? startRange = null;
        WordRange? nextPageRange = null;

        try
        {
            object what = WdGoToItem.wdGoToPage;
            object which = WdGoToDirection.wdGoToAbsolute;
            object count = pageNumber;
            object missing = Type.Missing;

            startRange = doc.GoTo(
                What: ref what,
                Which: ref which,
                Count: ref count,
                Name: ref missing);

            var start = startRange.Start;
            int end;

            if (pageNumber < totalPages)
            {
                object nextWhat = WdGoToItem.wdGoToPage;
                object nextWhich = WdGoToDirection.wdGoToAbsolute;
                object nextCount = pageNumber + 1;
                object nextMissing = Type.Missing;

                nextPageRange = doc.GoTo(
                    What: ref nextWhat,
                    Which: ref nextWhich,
                    Count: ref nextCount,
                    Name: ref nextMissing);

                end = nextPageRange.Start;
            }
            else
            {
                end = doc.Content.End;
            }

            return doc.Range(start, end);
        }
        finally
        {
            ReleaseComObject(startRange);
            ReleaseComObject(nextPageRange);
        }
    }

    private static int CalculateFragmentIndex(
        ParagraphInfo paragraph,
        int currentPageNumber,
        WordDocument doc)
    {
        WordRange? paragraphStartRange = null;

        try
        {
            paragraphStartRange = doc.Range(paragraph.Start, paragraph.Start);

            var pageObject = paragraphStartRange.Information[WdInformation.wdActiveEndAdjustedPageNumber];

            var startPage = pageObject is int value && value > 0
                ? value
                : currentPageNumber;

            return Math.Max(1, currentPageNumber - startPage + 1);
        }
        catch
        {
            return 1;
        }
        finally
        {
            ReleaseComObject(paragraphStartRange);
        }
    }

    private static bool RangesIntersect(
        int firstStart,
        int firstEnd,
        int secondStart,
        int secondEnd)
    {
        return firstStart < secondEnd && secondStart < firstEnd;
    }

    private static string GetStyleName(WordParagraph paragraph)
    {
        object? styleObject = null;

        try
        {
            styleObject = paragraph.get_Style();

            if (styleObject == null)
                return string.Empty;

            if (styleObject is WordStyle wordStyle)
                return NormalizeStyleName(wordStyle.NameLocal);

            return NormalizeStyleName(styleObject.ToString());
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            ReleaseComObject(styleObject);
        }
    }

    private static string CleanPageText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\r", "\n")
            .Replace("\a", string.Empty)
            .TrimEnd('\n', '\r');
    }

    private static string CleanParagraphFragmentText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\r", string.Empty)
            .Replace("\a", string.Empty)
            .Trim();
    }

    private static string CleanCellText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\r\a", string.Empty)
            .Replace("\r", " ")
            .Replace("\a", string.Empty)
            .Trim();
    }


    private static string NormalizeStyleName(string? styleName)
    {
        if (string.IsNullOrWhiteSpace(styleName))
            return string.Empty;

        return styleName.Trim();
    }

    private static void ReleaseComObject(object? comObject)
    {
        if (comObject == null)
            return;

        try
        {
            if (System.Runtime.InteropServices.Marshal.IsComObject(comObject))
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(comObject);
        }
        catch
        {
        }
    }

    private sealed class ParagraphInfo
    {
        public int ParagraphIndex { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public string StyleName { get; set; } = string.Empty;
    }

    private sealed class TableInfo
    {
        public int TableIndex { get; set; }

        public int Start { get; set; }

        public int End { get; set; }

        public List<TableRowInfo> Rows { get; set; } = new();
    }
    private sealed class TableRowInfo
    {
        public int RowIndex { get; set; }

        public int Start { get; set; }

        public int End { get; set; }

        public TableRowContent Content { get; set; } = new();
    }
    private sealed class OpenXmlImageInfo
    {
        public int ImageIndex { get; set; }

        public string RelationshipId { get; set; } = string.Empty;

        public string ContentType { get; set; } = "application/octet-stream";

        public byte[] Bytes { get; set; } = Array.Empty<byte>();
    }


    private sealed class ImageInfo
    {
        public int ImageIndex { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public string ContentType { get; set; } = "image/png";
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
    }
}

