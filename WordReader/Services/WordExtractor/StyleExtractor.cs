using System.Globalization;
using System.Threading.Tasks.Sources;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using WordReader.Services.Models;
using A = DocumentFormat.OpenXml.Drawing;
using MyStyle = WordReader.Services.Models;


namespace WordReader.Services.WordExtractor;

public interface IExtractStyleService
{
    IList<BookStyleEntity> Extract(string filePath);
}

public class ExtractStyleService : IExtractStyleService
{
    public IList<BookStyleEntity> Extract(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        var ext = Path.GetExtension(filePath);
        if (!string.Equals(ext, ".docx", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Accurate style extraction is supported for .docx files.");

        using WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false);
        // var themeColors = ReadThemeColors(doc);
        StyleDefinitionsPart? stylesPart = doc.MainDocumentPart?.StyleDefinitionsPart;
        var result = new Dictionary<string, MyStyle.BookStyleEntity>(StringComparer.OrdinalIgnoreCase);
        var styleList = new List<BookStyleEntity>();

        foreach (var s in stylesPart.Styles.Elements<Style>())
        {
            styleList.Add(ExtractStyle(s));
            
        }

        return styleList;

        // if (stylesPart?.Styles == null)
        // return result;

        // return ResolveInheritance(result);
    }

    private BookStyleEntity ExtractStyle(Style? s)
    {
        var style = new BookStyleEntity
        {
            StyleId = s.StyleId?.Value,
            Name = s.StyleName?.Val?.Value ?? s.StyleId?.Value ?? "UnnamedStyle",
            Type = s.Type?.Value.MapEnum<StyleValues, StyleType>(),
            BasedOnStyleId = s.BasedOn?.Val?.Value,
            // NextParagraphStyleId = s.NextParagraphStyle?.Style?.Value,
            // IsCustom = s.CustomStyle?.Value ?? false,
        };


        if (s.StyleParagraphProperties != null)
        {
            style.StyleParagraphProperties = MapStyleParagraphProps(s.StyleParagraphProperties);
        }

        if (s.StyleRunProperties != null)
        {
            style.StyleRunProperties = MapStyleRunProps(s.StyleRunProperties);
        }

        if (s.StyleTableProperties != null)
        {
            style.StyleTableProperties = MapStyleTableProps(s.StyleTableProperties);
        }

        return style;
    }

    private StyleTablePropertiesEntity? MapStyleTableProps(StyleTableProperties tp)
    {
        var result = new StyleTablePropertiesEntity();
        if (tp.Shading != null)
        {
            result.Shading = new ShadingModel();
            if (tp.Shading.Val != null)
                result.Shading.Val = MapShadingPattern(tp.Shading.Val.Value);
            result.Shading.Color =
                tp.Shading.Color != null ? tp.Shading.Color.Value : null;
            result.Shading.Fill =
                tp.Shading.Fill != null ? tp.Shading.Fill.Value : null;
            result.Shading.ThemeColor =
                tp.Shading.ThemeColor != null ? MapThemeColor(tp.Shading.ThemeColor.Value) : null;
            result.Shading.ThemeFill =
                tp.Shading.ThemeFill != null ? MapThemeColor(tp.Shading.ThemeFill.Value) : null;
            result.Shading.ThemeFillShade =
                tp.Shading.ThemeFillShade != null ? tp.Shading.ThemeFillShade.Value : null;
        }

        if (tp.TableBorders != null)
        {
            result.TableBorders = new TableBordersModel
            {
                BottomBorder = MapBorderType(tp.TableBorders.BottomBorder),
                EndBorder = MapBorderType(tp.TableBorders.EndBorder),
                InsideHorizontalBorder = MapBorderType(tp.TableBorders.InsideHorizontalBorder),
                InsideVerticalBorder = MapBorderType(tp.TableBorders.InsideVerticalBorder),
                LeftBorder = MapBorderType(tp.TableBorders.LeftBorder),
                RightBorder = MapBorderType(tp.TableBorders.RightBorder),
                StartBorder = MapBorderType(tp.TableBorders.StartBorder),
                TopBorder = MapBorderType(tp.TableBorders.TopBorder)
            };
        }

        if (tp.TableCellSpacing != null)
        {
            result.TableCellSpacing = new TableCellSpacingModel()
            {
                Type = tp.TableCellSpacing.Type?.Value.MapEnum<TableWidthUnitValues, TableWidthUnitKind>(),
                Width = tp.TableCellSpacing.Width != null ? tp.TableCellSpacing.Width.Value : null
            };
        }

        if (tp.TableIndentation != null)
        {
            result.TableIndentation = new TableIndentationModel()
            {
                Type = tp.TableIndentation.Type?.Value.MapEnum<TableWidthUnitValues, TableWidthUnitKind>(),
                Width = tp.TableIndentation.Width != null ? tp.TableIndentation.Width.Value : null
            };
        }

        if (tp.TableJustification != null)
        {
            result.TableJustification =
                tp.TableJustification.Val?.Value.MapEnum<TableRowAlignmentValues, TableRowAlignmentKind>();
        }

        if (tp.TableStyleRowBandSize != null)
        {
            result.TableStyleRowBandSize = tp.TableStyleRowBandSize.Val?.Value;
        }

        if (tp.TableStyleColumnBandSize != null)
        {
            result.TableStyleColumnBandSize = tp.TableStyleColumnBandSize.Val?.Value;
        }

        if (tp.TableCellMarginDefault != null)
        {
            result.TableCellMarginDefault = new TableCellMarginDefaultModel
            {
                BottomMargin = new TableCellSpacingModel()
                {
                    Type = tp.TableCellMarginDefault.BottomMargin?.Type?.Value
                        .MapEnum<TableWidthUnitValues, TableWidthUnitKind>(),
                    Width = tp.TableCellMarginDefault.BottomMargin?.Width
                },
                EndMargin = new TableCellSpacingModel
                {
                    Type = tp.TableCellMarginDefault.EndMargin?.Type?.Value
                        .MapEnum<TableWidthUnitValues, TableWidthUnitKind>(),
                    Width = tp.TableCellMarginDefault.EndMargin?.Width
                },
                StartMargin = new TableCellSpacingModel
                {
                    Type = tp.TableCellMarginDefault.StartMargin?.Type?.Value
                        .MapEnum<TableWidthUnitValues, TableWidthUnitKind>(),
                    Width = tp.TableCellMarginDefault.StartMargin?.Width
                },
                TopMargin = new TableCellSpacingModel
                {
                    Type = tp.TableCellMarginDefault.TopMargin?.Type?.Value
                        .MapEnum<TableWidthUnitValues, TableWidthUnitKind>(),
                    Width = tp.TableCellMarginDefault.TopMargin?.Width
                },
                TableCellLeftMarginModel = new TableWidthDxaNilTypeModel
                {
                    Type = tp.TableCellMarginDefault.TableCellLeftMargin?.Type?.Value
                        .MapEnum<TableWidthValues, TableWidthKind>(),
                    Width = tp.TableCellMarginDefault.TableCellLeftMargin?.Width?.Value
                },
                TableCellRightMarginModel = new TableWidthDxaNilTypeModel
                {
                    Type = tp.TableCellMarginDefault.TableCellRightMargin?.Type?.Value
                        .MapEnum<TableWidthValues, TableWidthKind>(),
                    Width = tp.TableCellMarginDefault.TableCellRightMargin?.Width?.Value
                }
            };
        }

        return result;
    }

    private static StyleRunPropertiesEntity? MapStyleRunProps(StyleRunProperties rp)
    {
        var result = new StyleRunPropertiesEntity();
        if (rp.Shading != null)
        {
            result.Shading = new ShadingModel();
            if (rp.Shading.Val != null)
                result.Shading.Val = MapShadingPattern(rp.Shading.Val.Value);
            result.Shading.Color = rp.Shading.Color != null ? rp.Shading.Color.Value : null;
            result.Shading.Fill = rp.Shading.Fill != null ? rp.Shading.Fill.Value : null;
            result.Shading.ThemeColor =
                rp.Shading.ThemeColor != null ? MapThemeColor(rp.Shading.ThemeColor.Value) : null;
            result.Shading.ThemeFill =
                rp.Shading.ThemeFill != null ? MapThemeColor(rp.Shading.ThemeFill.Value) : null;
            result.Shading.ThemeFillShade =
                rp.Shading.ThemeFillShade != null ? rp.Shading.ThemeFillShade.Value : null;
        }

        if (rp.RunFonts != null)
        {
            result.RunFonts = new RunFontsModel();
            result.RunFonts.Ascii = rp.RunFonts.Ascii != null ? rp.RunFonts.Ascii.Value : null;
            result.RunFonts.ComplexScript = rp.RunFonts.ComplexScript != null ? rp.RunFonts.ComplexScript.Value : null;
            result.RunFonts.EastAsia = rp.RunFonts.EastAsia != null ? rp.RunFonts.EastAsia.Value : null;
            result.RunFonts.HighAnsi = rp.RunFonts.HighAnsi != null ? rp.RunFonts.HighAnsi.Value : null;
            result.RunFonts.Hint = rp.RunFonts.Hint != null ? MapRunFontHint(rp.RunFonts.Hint.Value) : null;
            result.RunFonts.AsciiTheme = rp.RunFonts.AsciiTheme != null
                ? MapRunFontThemeFont(rp.RunFonts.AsciiTheme.Value)
                : null;
            result.RunFonts.ComplexScriptTheme = rp.RunFonts.ComplexScriptTheme != null
                ? MapRunFontThemeFont(rp.RunFonts.ComplexScriptTheme.Value)
                : null;
            result.RunFonts.EastAsiaTheme = rp.RunFonts.EastAsiaTheme != null
                ? MapRunFontThemeFont(rp.RunFonts.EastAsiaTheme.Value)
                : null;
            result.RunFonts.HighAnsiTheme = rp.RunFonts.HighAnsiTheme != null
                ? MapRunFontThemeFont(rp.RunFonts.HighAnsiTheme.Value)
                : null;
        }

        if (rp.Border != null)
        {
            result.Border = new BorderModel
            {
                ThemeColor = rp.Border.ThemeColor != null ? MapThemeColor(rp.Border.ThemeColor.Value) : null,
                Color = rp.Border.Color != null ? rp.Border.Color.Value : null,
                Frame = rp.Border.Frame != null ? OnOffToBool(rp.Border.Frame.Value) : null,
                Shadow = rp.Border.Shadow != null ? OnOffToBool(rp.Border.Shadow.Value) : null,
                Size = rp.Border.Size != null ? rp.Border.Size.Value : null,
                Space = rp.Border.Space != null ? rp.Border.Space.Value : null,
                ThemeShade = rp.Border.ThemeShade != null ? rp.Border.ThemeShade.Value : null,
                ThemeTint = rp.Border.ThemeTint != null ? rp.Border.ThemeTint.Value : null,
                Val = rp.Border.Val?.Value.MapEnum<BorderValues, BorderKind>()
            };
        }

        result.CharacterScale = rp.CharacterScale != null && rp.CharacterScale.Val != null
            ? rp.CharacterScale.Val.Value
            : null;

        result.DoubleStrike = rp.DoubleStrike != null ? OnOffToBool(rp.DoubleStrike.Val) : null;
        result.Bold = rp.Bold != null ? OnOffToBool(rp.Bold.Val) : null;
        result.Strike = rp.Strike != null ? OnOffToBool(rp.Strike.Val) : null;
        result.Caps = rp.Caps != null ? OnOffToBool(rp.Caps.Val) : null;
        result.Emboss = rp.Emboss != null ? OnOffToBool(rp.Emboss.Val) : null;
        result.Imprint = rp.Imprint != null ? OnOffToBool(rp.Imprint.Val) : null;
        result.Italic = rp.Italic != null ? OnOffToBool(rp.Italic.Val) : null;
        result.Shadow = rp.Shadow != null ? OnOffToBool(rp.Shadow.Val) : null;
        result.Outline = rp.Outline != null ? OnOffToBool(rp.Outline.Val) : null;
        result.SnapToGrid = rp.SnapToGrid != null ? OnOffToBool(rp.SnapToGrid.Val) : null;
        result.NoProof = rp.NoProof != null ? OnOffToBool(rp.NoProof.Val) : null;
        result.Vanish = rp.Vanish != null ? OnOffToBool(rp.Vanish.Val) : null;
        result.WebHidden = rp.WebHidden != null ? OnOffToBool(rp.WebHidden.Val) : null;
        result.SpecVanish = rp.SpecVanish != null ? OnOffToBool(rp.SpecVanish.Val) : null;
        result.SmallCaps = rp.SmallCaps != null ? OnOffToBool(rp.SmallCaps.Val) : null;
        if (rp.Color != null)
        {
            result.Color = new ColorModel
            {
                ThemeTint = rp.Color.ThemeTint != null ? rp.Color.ThemeTint.Value : null,
                Val = rp.Color.Val != null ? rp.Color.Val.Value : null,
                ThemeColor = rp.Color.ThemeColor?.Value.MapEnum<ThemeColorValues, ThemeColorKind>(),
                ThemeShade = rp.Color.ThemeShade != null ? rp.Color.ThemeShade.Value : null
            };
        }

        result.Spacing = (rp.Spacing != null && rp.Spacing.Val != null) ? rp.Spacing.Val.Value : null;
        result.Position = rp.Position != null && rp.Position.Val != null ? rp.Position.Val.Value : null;
        if (rp.FontSize?.Val != null && double.TryParse(rp.FontSize.Val.Value, out var fontSizeSz))
        {
            result.FontSize = (fontSizeSz / 2d).ToString(CultureInfo.InvariantCulture);
        }

        if (rp.FontSizeComplexScript?.Val != null &&
            double.TryParse(rp.FontSizeComplexScript.Val.Value, out var fontSizeCsSz))
        {
            result.FontSizeComplexScript = (fontSizeCsSz / 2d).ToString(CultureInfo.InvariantCulture);
        }

        result.Kern = rp.Kern != null && rp.Kern.Val != null ? rp.Kern.Val.Value : null;
        if (rp.Underline != null)
        {
            result.Underline = new UnderlineModel
            {
                Color = rp.Underline.Color != null ? rp.Underline.Color.Value : null,
                ThemeColor = rp.Underline.ThemeColor?.Value.MapEnum<ThemeColorValues, ThemeColorKind>(),
                ThemeShade = rp.Underline.ThemeShade != null ? rp.Underline.ThemeShade.Value : null,
                ThemeTint = rp.Underline.ThemeTint != null ? rp.Underline.ThemeTint.Value : null,
                Val = rp.Underline.Val?.Value.MapEnum<UnderlineValues, UnderlineKind>()
            };
        }

        if (rp.TextEffect != null)
        {
            result.TextEffect = rp.TextEffect.Val?.Value.MapEnum<TextEffectValues, TextEffectKind>();
        }

        if (rp.FitText != null)
        {
            result.FitText = new FitTextModel
            {
                Id = rp.FitText.Id != null ? rp.FitText.Id.Value : null,
                Val = rp.FitText.Val != null ? rp.FitText.Val.Value : null
            };
        }

        if (rp.VerticalTextAlignment != null)
        {
            result.VerticalTextAlignment = rp.VerticalTextAlignment.Val?.Value
                .MapEnum<VerticalPositionValues, VerticalPositionKind>();
        }

        if (rp.Emphasis != null)
        {
            result.Emphasis = rp.Emphasis.Val?.Value.MapEnum<EmphasisMarkValues, EmphasisMarkKind>();
        }

        if (rp.Languages != null)
        {
            result.Languages = new LanguageModel
            {
                Bidi = rp.Languages.Bidi != null ? rp.Languages.Bidi.Value : null,
                EastAsia = rp.Languages.EastAsia != null ? rp.Languages.EastAsia.Value : null,
                Val = rp.Languages.Val != null ? rp.Languages.Val.Value : null
            };
        }

        if (rp.EastAsianLayout != null)
        {
            result.EastAsianLayout = new EastAsianLayoutModel
            {
                Combine = rp.EastAsianLayout.Combine != null ? OnOffToBool(rp.EastAsianLayout.Combine.Value) : null,
                Id = rp.EastAsianLayout.Id != null ? rp.EastAsianLayout.Id.Value : null,
                CombineBrackets = rp.EastAsianLayout.CombineBrackets?.Value
                    .MapEnum<CombineBracketValues, CombineBracketKind>()
            };
        }


        return result;
    }

    private static ThemeFontKind? MapRunFontThemeFont(ThemeFontValues value)
    {
        return value.MapEnum<ThemeFontValues, ThemeFontKind>();
    }

    private static FontTypeHintKind MapRunFontHint(FontTypeHintValues value)
    {
        return value.MapEnum<FontTypeHintValues, FontTypeHintKind>();
    }

    // private FontStyleInfo ExtractFont(StyleRunProperties rp, Dictionary<string, string> themeColors)
    // {
    //     var result = new FontStyleInfo();
    //
    //     var fonts = rp.RunFonts;
    //     if (fonts != null)
    //     {
    //         result.LatinFontName = fonts.Ascii?.Value;
    //         result.ComplexScriptFontName = fonts.ComplexScript?.Value;
    //         result.EastAsiaFontName = fonts.EastAsia?.Value;
    //     }
    //
    //     if (rp.FontSize?.Val != null && double.TryParse(rp.FontSize.Val.Value, out var szHalfPt))
    //         result.SizePt = szHalfPt / 2d;
    //
    //     if (rp.FontSizeComplexScript?.Val != null &&
    //         double.TryParse(rp.FontSizeComplexScript.Val.Value, out var csHalfPt))
    //         result.ComplexScriptSizePt = csHalfPt / 2d;
    //
    //     result.Bold = rp.Bold != null ? OnOffToBool(rp.Bold.Val) : null;
    //     result.Italic = rp.Italic != null ? OnOffToBool(rp.Italic.Val) : null;
    //     result.BoldComplexScript = rp.BoldComplexScript != null ? OnOffToBool(rp.BoldComplexScript.Val) : null;
    //     result.ItalicComplexScript = rp.ItalicComplexScript != null ? OnOffToBool(rp.ItalicComplexScript.Val) : null;
    //
    //     result.Strike = rp.Strike != null ? OnOffToBool(rp.Strike.Val) : null;
    //     result.DoubleStrike = rp.DoubleStrike != null ? OnOffToBool(rp.DoubleStrike.Val) : null;
    //     result.SmallCaps = rp.SmallCaps != null ? OnOffToBool(rp.SmallCaps.Val) : null;
    //     result.AllCaps = rp.Caps != null ? OnOffToBool(rp.Caps.Val) : null;
    //     result.Hidden = rp.Vanish != null ? OnOffToBool(rp.Vanish.Val) : null;
    //
    //     if (rp.VerticalTextAlignment != null)
    //     {
    //         result.Superscript = rp.VerticalTextAlignment.Val == VerticalPositionValues.Superscript;
    //         result.Subscript = rp.VerticalTextAlignment.Val == VerticalPositionValues.Subscript;
    //     }
    //
    //     if (rp.Underline != null)
    //     {
    //         result.UnderlineStyle = MapUnderline(rp.Underline.Val?.Value);
    //         result.UnderlineColor = ResolveColor(
    //             rp.Underline.Color?.Value,
    //             rp.Underline.ThemeColor?.Value.ToString(),
    //             rp.Underline.ThemeTint?.Value,
    //             rp.Underline.ThemeShade?.Value,
    //             themeColors);
    //     }
    //
    //     if (rp.Color != null)
    //     {
    //         var hex = ResolveColor(
    //             rp.Color.Val?.Value,
    //             rp.Color.ThemeColor?.Value.ToString(),
    //             rp.Color.ThemeTint?.Value,
    //             rp.Color.ThemeShade?.Value,
    //             themeColors);
    //
    //         result.Color = hex;
    //         result.ColorRgb = HexToRgbInt(hex);
    //     }
    //
    //     //if (rp.Highlight != null)
    //     //{
    //     //    result.HighlightColor = MapHighlightToHex(rp.Highlight.Style?.Value);
    //     //}
    //
    //     return result;
    // }

    private StyleParagraphPropertiesEntity MapStyleParagraphProps(StyleParagraphProperties pp)
    {
        var result = new StyleParagraphPropertiesEntity();

        if (pp.Justification is { Val: not null })
            result.Justification = MapJustification(pp.Justification.Val.Value);
        if (pp.TextAlignment is { Val: not null })
            result.TextAlignment = MapTextAlignment(pp.TextAlignment.Val.Value);

        if (pp.OutlineLevel?.Val != null)
            result.OutlineLevel = MapOutlineLevel(pp.OutlineLevel.Val.Value);

        if (pp.Tabs != null)
        {
            result.Tabs = [];
            foreach (var tab in pp.Tabs.Elements<TabStop>())
            {
                result.Tabs.Add(new TabStopModel()
                {
                    Alignment = tab.Val != null ? MapTabStopAlignment(tab.Val.Value) : null,
                    Leader = tab.Leader != null ? MapTabStopLeader(tab.Leader.Value) : null,
                    Position = tab.Position != null ? TwipToPt(tab.Position.Value) : null
                });
            }
        }

        result.WordWrap = pp.WordWrap != null ? OnOffToBool(pp.WordWrap.Val) : null;
        result.AdjustRightIndent = pp.AdjustRightIndent != null ? OnOffToBool(pp.AdjustRightIndent.Val) : null;
        result.AutoSpaceDE = pp.AutoSpaceDE != null ? OnOffToBool(pp.AutoSpaceDE.Val) : null;
        result.AutoSpaceDN = pp.AutoSpaceDN != null ? OnOffToBool(pp.AutoSpaceDN.Val) : null;
        result.KeepLines = pp.KeepLines != null ? OnOffToBool(pp.KeepLines.Val) : null;
        result.KeepNext = pp.KeepNext != null ? OnOffToBool(pp.KeepNext.Val) : null;
        result.Kinsoku = pp.Kinsoku != null ? OnOffToBool(pp.Kinsoku.Val) : null;
        result.MirrorIndents = pp.MirrorIndents != null ? OnOffToBool(pp.MirrorIndents.Val) : null;
        result.PageBreakBefore = pp.PageBreakBefore != null ? OnOffToBool(pp.PageBreakBefore.Val) : null;
        result.WidowControl = pp.WidowControl != null ? OnOffToBool(pp.WidowControl.Val) : null;
        result.SuppressAutoHyphens = pp.SuppressAutoHyphens != null ? OnOffToBool(pp.SuppressAutoHyphens.Val) : null;
        result.SuppressLineNumbers = pp.SuppressLineNumbers != null ? OnOffToBool(pp.SuppressLineNumbers.Val) : null;
        result.OverflowPunctuation = pp.OverflowPunctuation != null ? OnOffToBool(pp.OverflowPunctuation.Val) : null;
        result.TopLinePunctuation = pp.TopLinePunctuation != null ? OnOffToBool(pp.TopLinePunctuation.Val) : null;
        result.BiDi = pp.BiDi != null ? OnOffToBool(pp.BiDi.Val) : null;
        result.SnapToGrid = pp.SnapToGrid != null ? OnOffToBool(pp.SnapToGrid.Val) : null;
        result.SuppressOverlap = pp.SuppressOverlap != null ? OnOffToBool(pp.SuppressOverlap.Val) : null;
        result.ContextualSpacing = pp.ContextualSpacing != null ? OnOffToBool(pp.ContextualSpacing.Val) : null;
        if (pp.FrameProperties != null)
        {
            result.FrameProperties = new()
            {
                AnchorLock = pp.FrameProperties.AnchorLock != null
                    ? OnOffToBool(pp.FrameProperties.AnchorLock)
                    : null,
            };
            if (pp.FrameProperties.DropCap != null)
                result.FrameProperties.DropCap = MapFramePropertiesDropCap(pp.FrameProperties.DropCap.Value);
            if (pp.FrameProperties.Height != null)
                result.FrameProperties.Height = pp.FrameProperties.Height.Value;
            if (pp.FrameProperties.HeightType != null)
                result.FrameProperties.HeightType = MapFramePropertiesHeightType(pp.FrameProperties.HeightType.Value);
            if (pp.FrameProperties.HorizontalPosition != null)
                result.FrameProperties.HorizontalPosition =
                    MapFramePropertiesHorizontalPosition(pp.FrameProperties.HorizontalPosition.Value);
            if (pp.FrameProperties.HorizontalSpace != null)
                result.FrameProperties.HorizontalSpace = pp.FrameProperties.HorizontalSpace.Value;
            if (pp.FrameProperties.Lines != null)
                result.FrameProperties.Lines = pp.FrameProperties.Lines.Value;
            if (pp.FrameProperties.VerticalPosition != null)
                result.FrameProperties.VerticalPosition =
                    MapFramePropertiesVerticalPosition(pp.FrameProperties.VerticalPosition.Value);
            if (pp.FrameProperties.VerticalSpace != null)
                result.FrameProperties.VerticalSpace = pp.FrameProperties.VerticalSpace.Value;
            if (pp.FrameProperties.Width != null)
                result.FrameProperties.Width = pp.FrameProperties.Width.Value;

            if (pp.FrameProperties.Wrap != null)
                result.FrameProperties.Wrap = MapFramePropertiesWrap(pp.FrameProperties.Wrap.Value);
            if (pp.FrameProperties.X != null)
                result.FrameProperties.X = pp.FrameProperties.X.Value;
            if (pp.FrameProperties.XAlign != null)
                result.FrameProperties.XAlign = MapFramePropertiesXAlignment(pp.FrameProperties.XAlign.Value);
            if (pp.FrameProperties.Y != null)
                result.FrameProperties.Y = pp.FrameProperties.Y.Value;

            result.FrameProperties.YAlign = pp.FrameProperties.YAlign != null
                ? MapFramePropertiesYAlignment(pp.FrameProperties.YAlign.Value)
                : null;
        }

        if (pp.Shading != null)
        {
            result.Shading = new ShadingModel();
            if (pp.Shading.Val != null)
                result.Shading.Val = MapShadingPattern(pp.Shading.Val.Value);
            if (pp.Shading.Color != null)
            {
                result.Shading.Color = pp.Shading.Color.Value;
            }

            if (pp.Shading.Fill != null)
            {
                result.Shading.Fill = pp.Shading.Fill.Value;
            }

            if (pp.Shading.ThemeFillShade != null)
            {
                result.Shading.ThemeFillShade = pp.Shading.ThemeFillShade.Value;
            }

            if (pp.Shading.ThemeShade != null)
            {
                result.Shading.ThemeShade = pp.Shading.ThemeShade.Value;
            }

            if (pp.Shading.ThemeTint != null)
            {
                result.Shading.ThemeTint = pp.Shading.ThemeTint.Value;
            }

            if (pp.Shading.ThemeFillTint != null)
            {
                result.Shading.ThemeFillTint = pp.Shading.ThemeFillTint.Value;
            }

            if (pp.Shading.ThemeColor != null)
            {
                result.Shading.ThemeColor = MapThemeColor(pp.Shading.ThemeColor.Value);
            }

            if (pp.Shading.ThemeFill != null)
                result.Shading.ThemeFill = MapThemeColor(pp.Shading.ThemeFill.Value);
        }

        if (pp.TextBoxTightWrap is { Val: not null })
            result.TextBoxTightWrap =
                pp.TextBoxTightWrap.Val.Value.MapEnum<TextBoxTightWrapValues, TextBoxTightWrapKind>();


        if (pp.Indentation != null)
        {
            result.Indentation = new IndentationModel
            {
                Left = TwipToPt(pp.Indentation.Left?.Value),
                Right = TwipToPt(pp.Indentation.Right?.Value),
                FirstLine = TwipToPt(pp.Indentation.FirstLine?.Value),
                Hanging = TwipToPt(pp.Indentation.Hanging?.Value),
                Start = TwipToPt(pp.Indentation.Start?.Value),
                End = TwipToPt(pp.Indentation.End?.Value)
            };
        }

        if (pp.SpacingBetweenLines != null)
        {
            result.Spacing = new SpacingBetweenLinesModel
            {
                Before = TwipToPt(pp.SpacingBetweenLines.Before?.Value),
                After = TwipToPt(pp.SpacingBetweenLines.After?.Value),
            };

            if (pp.SpacingBetweenLines.LineRule != null)
                result.Spacing.LineRule = MapLineSpacingType(pp.SpacingBetweenLines.LineRule.Value);

            if (pp.SpacingBetweenLines.Line != null)
                result.Spacing.Line = LineToDisplayValue(
                    pp.SpacingBetweenLines.Line.Value,
                    pp.SpacingBetweenLines.LineRule?.Value);
        }

        if (pp.ParagraphBorders != null)
        {
            result.ParagraphBorders = ExtractBorder(pp);
        }


        return result;
    }


    private static ShadingPatternKind? MapShadingPattern(ShadingPatternValues value)
    {
        return value.MapEnum<ShadingPatternValues, ShadingPatternKind>();
    }

    private static VerticalAlignmentKind? MapFramePropertiesYAlignment(VerticalAlignmentValues value)
    {
        return value.MapEnum<VerticalAlignmentValues, VerticalAlignmentKind>();
    }

    private static HorizontalAlignmentKind? MapFramePropertiesXAlignment(HorizontalAlignmentValues value)
    {
        return value.MapEnum<HorizontalAlignmentValues, HorizontalAlignmentKind>();
    }

    private static TextWrappingKind? MapFramePropertiesWrap(TextWrappingValues value)
    {
        return value.MapEnum<TextWrappingValues, TextWrappingKind>();
    }

    private static VerticalAnchorKind? MapFramePropertiesVerticalPosition(VerticalAnchorValues value)
    {
        return value.MapEnum<VerticalAnchorValues, VerticalAnchorKind>();
    }

    private static HorizontalAnchorKind? MapFramePropertiesHorizontalPosition(HorizontalAnchorValues value)
    {
        return value.MapEnum<HorizontalAnchorValues, HorizontalAnchorKind>();
    }

    private static HeightRuleKind? MapFramePropertiesHeightType(HeightRuleValues value)
    {
        return value.MapEnum<HeightRuleValues, HeightRuleKind>();
    }

    private static DropCapLocationKind? MapFramePropertiesDropCap(DropCapLocationValues dropCapValue)
    {
        return dropCapValue.MapEnum<DropCapLocationValues, DropCapLocationKind>();
    }

    private static TabLeaderKind? MapTabStopLeader(TabStopLeaderCharValues leader)
    {
        return leader.MapEnum<TabStopLeaderCharValues, TabLeaderKind>();
    }

    private static TabAlignmentKind? MapTabStopAlignment(TabStopValues alignment)
    {
        return alignment.MapEnum<TabStopValues, TabAlignmentKind>();
    }

    private static TextAlignmentKind? MapTextAlignment(VerticalTextAlignmentValues value)
    {
        return value.MapEnum<VerticalTextAlignmentValues, TextAlignmentKind>();
    }

    private static ParagraphBordersModel ExtractBorder(StyleParagraphProperties pp)
    {
        var result = new ParagraphBordersModel();
        var borders = pp.ParagraphBorders;
        if (borders == null)
            return result;


        result.Top = MapBorderType(borders.TopBorder);
        result.Bottom = MapBorderType(borders.BottomBorder);
        result.Left = MapBorderType(borders.LeftBorder);
        result.Right = MapBorderType(borders.RightBorder);
        result.Bar = MapBorderType(borders.BarBorder);
        result.Between = MapBorderType(borders.BetweenBorder);


        return result;
    }

    private static BorderModel? MapBorderType(BorderType? border)
    {
        if (border == null)
            return null;
        var result = new BorderModel
        {
            Val = border.Val?.Value.MapEnum<BorderValues, BorderKind>(),
            Space = border.Space != null ? border.Space.Value : null,
            ThemeColor = border.ThemeColor?.Value.MapEnum<ThemeColorValues, ThemeColorKind>(),
            Color = border.Color?.Value, 
            Shadow = border.Shadow != null ? OnOffToBool(border.Shadow.Value) : null,
            Frame = border.Frame != null ? OnOffToBool(border.Frame.Value) : null,
            ThemeShade = border.ThemeShade != null ? border.ThemeShade.Value : null,
            ThemeTint = border.ThemeTint != null ? border.ThemeTint.Value : null
        };


        if (border.Size?.Value != null)
            result.Size = border.Size.Value / 2;

        return result;
    }

    private static ThemeColorKind? MapThemeColor(ThemeColorValues tc)
    {
        return tc.MapEnum<ThemeColorValues, ThemeColorKind>();
    }

    private Dictionary<string, MyStyle.BookStyleEntity> ResolveInheritance(
        Dictionary<string, MyStyle.BookStyleEntity> styles)
    {
        var resolved = new Dictionary<string, MyStyle.BookStyleEntity>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in styles.Values)
            resolved[item.Name] = ResolveStyle(item, styles, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        return resolved;
    }

    private MyStyle.BookStyleEntity ResolveStyle(MyStyle.BookStyleEntity bookStyleEntity,
        Dictionary<string, MyStyle.BookStyleEntity> styles,
        HashSet<string> chain)
    {
        if (string.IsNullOrWhiteSpace(bookStyleEntity.BasedOnStyleId))
            return bookStyleEntity;

        if (!chain.Add(bookStyleEntity.Name))
            return bookStyleEntity;

        var parent = styles.Values.FirstOrDefault(x =>
            string.Equals(x.StyleId, bookStyleEntity.BasedOnStyleId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Name, bookStyleEntity.BasedOnStyleId, StringComparison.OrdinalIgnoreCase));

        if (parent == null)
            return bookStyleEntity;

        parent = ResolveStyle(parent, styles, chain);

        // return Merge(parent, bookStyleEntity);
        return null;
    }

    // private MyStyle.BookStyleEntity Merge(MyStyle.BookStyleEntity parent, MyStyle.BookStyleEntity child)
    // {
    //     return new MyStyle.BookStyleEntity
    //     {
    //         Name = child.Name,
    //         StyleId = child.StyleId,
    //         Type = child.Type,
    //         BasedOnStyleId = child.BasedOnStyleId,
    //         NextParagraphStyleId = child.NextParagraphStyleId,
    //         IsCustom = child.IsCustom,
    //
    //         Font = MergeFont(parent.Font, child.Font),
    //         Paragraph = MergeParagraph(parent.Paragraph, child.Paragraph),
    //         Border = MergeBorder(parent.Border, child.Border)
    //     };
    // }
    //
    // private FontStyleInfo MergeFont(FontStyleInfo parent, FontStyleInfo child)
    // {
    //     return new FontStyleInfo
    //     {
    //         LatinFontName = child.LatinFontName ?? parent.LatinFontName,
    //         ComplexScriptFontName = child.ComplexScriptFontName ?? parent.ComplexScriptFontName,
    //         EastAsiaFontName = child.EastAsiaFontName ?? parent.EastAsiaFontName,
    //         SizePt = child.SizePt ?? parent.SizePt,
    //         ComplexScriptSizePt = child.ComplexScriptSizePt ?? parent.ComplexScriptSizePt,
    //         Bold = child.Bold ?? parent.Bold,
    //         Italic = child.Italic ?? parent.Italic,
    //         BoldComplexScript = child.BoldComplexScript ?? parent.BoldComplexScript,
    //         ItalicComplexScript = child.ItalicComplexScript ?? parent.ItalicComplexScript,
    //         UnderlineStyle = child.UnderlineStyle != UnderlineStyle.None
    //             ? child.UnderlineStyle
    //             : parent.UnderlineStyle,
    //         Strike = child.Strike ?? parent.Strike,
    //         DoubleStrike = child.DoubleStrike ?? parent.DoubleStrike,
    //         SmallCaps = child.SmallCaps ?? parent.SmallCaps,
    //         AllCaps = child.AllCaps ?? parent.AllCaps,
    //         Hidden = child.Hidden ?? parent.Hidden,
    //         Superscript = child.Superscript ?? parent.Superscript,
    //         Subscript = child.Subscript ?? parent.Subscript,
    //         Color = child.Color ?? parent.Color,
    //         ColorRgb = child.ColorRgb ?? parent.ColorRgb,
    //         HighlightColor = child.HighlightColor ?? parent.HighlightColor,
    //         UnderlineColor = child.UnderlineColor ?? parent.UnderlineColor
    //     };
    // }
    //
    // private ParagraphStyleInfo MergeParagraph(ParagraphStyleInfo parent, ParagraphStyleInfo child)
    // {
    //     return new ParagraphStyleInfo
    //     {
    //         Alignment = child.Alignment ?? parent.Alignment,
    //         OutlineLevelModel = child.OutlineLevelModel ?? parent.OutlineLevelModel,
    //         Direction = child.Direction ?? parent.Direction,
    //         LeftIndentPt = child.LeftIndentPt ?? parent.LeftIndentPt,
    //         RightIndentPt = child.RightIndentPt ?? parent.RightIndentPt,
    //         FirstLineIndentPt = child.FirstLineIndentPt ?? parent.FirstLineIndentPt,
    //         HangingIndentPt = child.HangingIndentPt ?? parent.HangingIndentPt,
    //         SpacingBeforePt = child.SpacingBeforePt ?? parent.SpacingBeforePt,
    //         SpacingAfterPt = child.SpacingAfterPt ?? parent.SpacingAfterPt,
    //         LineSpacingType = child.LineSpacingType ?? parent.LineSpacingType,
    //         LineSpacing = child.LineSpacing ?? parent.LineSpacing,
    //         KeepNext = child.KeepNext ?? parent.KeepNext,
    //         KeepLinesTogether = child.KeepLinesTogether ?? parent.KeepLinesTogether,
    //         PageBreakBefore = child.PageBreakBefore ?? parent.PageBreakBefore,
    //         WidowControl = child.WidowControl ?? parent.WidowControl
    //     };
    // }
    //
    // private BorderStyleInfo MergeBorder(BorderStyleInfo parent, BorderStyleInfo child)
    // {
    //     return new BorderStyleInfo
    //     {
    //         TopStyle = child.TopStyle != BorderStyleKind.None ? child.TopStyle : parent.TopStyle,
    //         BottomStyle = child.BottomStyle != BorderStyleKind.None ? child.BottomStyle : parent.BottomStyle,
    //         LeftStyle = child.LeftStyle != BorderStyleKind.None ? child.LeftStyle : parent.LeftStyle,
    //         RightStyle = child.RightStyle != BorderStyleKind.None ? child.RightStyle : parent.RightStyle,
    //
    //         TopWidthPt = child.TopWidthPt ?? parent.TopWidthPt,
    //         BottomWidthPt = child.BottomWidthPt ?? parent.BottomWidthPt,
    //         LeftWidthPt = child.LeftWidthPt ?? parent.LeftWidthPt,
    //         RightWidthPt = child.RightWidthPt ?? parent.RightWidthPt,
    //
    //         TopColor = child.TopColor ?? parent.TopColor,
    //         BottomColor = child.BottomColor ?? parent.BottomColor,
    //         LeftColor = child.LeftColor ?? parent.LeftColor,
    //         RightColor = child.RightColor ?? parent.RightColor
    //     };
    // }

    private Dictionary<string, string> ReadThemeColors(WordprocessingDocument doc)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var themePart = doc.MainDocumentPart?.ThemePart;
        var theme = themePart?.Theme;
        var colorScheme = theme?.ThemeElements?.ColorScheme;
        if (colorScheme == null)
            return result;

        foreach (var child in colorScheme.ChildElements)
        {
            string name = child.LocalName;
            string? value = null;

            var rgbModel = child.Descendants<A.RgbColorModelHex>().FirstOrDefault();
            if (rgbModel?.Val != null)
                value = NormalizeHex(rgbModel.Val.Value);

            var sysColor = child.Descendants<A.SystemColor>().FirstOrDefault();
            if (value == null && sysColor?.LastColor != null)
                value = NormalizeHex(sysColor.LastColor.Value);

            if (value != null)
                result[name] = value;
        }

        return result;
    }

    private string? ResolveColor(string? direct, string? themeColor, string? themeTint, string? themeShade,
        Dictionary<string, string> themeColors)
    {
        string? baseHex = null;

        if (!string.IsNullOrWhiteSpace(direct) &&
            !string.Equals(direct, "auto", StringComparison.OrdinalIgnoreCase))
        {
            baseHex = NormalizeHex(direct);
        }
        else if (!string.IsNullOrWhiteSpace(themeColor) && themeColors.TryGetValue(themeColor, out var themeHex))
        {
            baseHex = themeHex;
        }

        if (baseHex == null)
            return null;

        if (!string.IsNullOrWhiteSpace(themeTint))
            baseHex = ApplyTint(baseHex, themeTint);

        if (!string.IsNullOrWhiteSpace(themeShade))
            baseHex = ApplyShade(baseHex, themeShade);

        return baseHex;
    }

    private static string? NormalizeHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim().TrimStart('#');

        if (value.Length == 6)
            return "#" + value.ToUpperInvariant();

        return null;
    }

    private static int? HexToRgbInt(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        hex = hex.TrimStart('#');
        if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            return rgb;

        return null;
    }

    private static double? TwipToPt(int? value) => value / 20d;

    private static double? TwipToPt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return null;

        return v / 20d;
    }

    private static double? LineToDisplayValue(string? value, LineSpacingRuleValues? rule)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return null;

        if (rule == LineSpacingRuleValues.Auto)
            return v / 240d;

        return v / 20d;
    }

    private static bool OnOffToBool(OnOffValue? value)
    {
        return value == null || value.Value;
    }


    private static JustificationKind? MapJustification(JustificationValues value)
    {
        return value.MapEnum<JustificationValues, JustificationKind>();
    }

    private static OutlineLevelModel? MapOutlineLevel(int value)
    {
        return value switch
        {
            0 => OutlineLevelModel.Level1,
            1 => OutlineLevelModel.Level2,
            2 => OutlineLevelModel.Level3,
            3 => OutlineLevelModel.Level4,
            4 => OutlineLevelModel.Level5,
            5 => OutlineLevelModel.Level6,
            6 => OutlineLevelModel.Level7,
            7 => OutlineLevelModel.Level8,
            8 => OutlineLevelModel.Level9,
            _ => OutlineLevelModel.BodyText
        };
    }

    private static LineSpacingRuleKind? MapLineSpacingType(LineSpacingRuleValues value)
    {
        return value.MapEnum<LineSpacingRuleValues, LineSpacingRuleKind>();
    }

    // private static UnderlineStyle MapUnderline(UnderlineValues? value)
    // {
    //     if (value == UnderlineValues.Single)
    //         return UnderlineStyle.Single;
    //
    //     if (value == UnderlineValues.Words)
    //         return UnderlineStyle.Words;
    //
    //     if (value == UnderlineValues.Double)
    //         return UnderlineStyle.Double;
    //
    //     if (value == UnderlineValues.Thick)
    //         return UnderlineStyle.Thick;
    //
    //     if (value == UnderlineValues.Dotted)
    //         return UnderlineStyle.Dotted;
    //
    //     if (value == UnderlineValues.DottedHeavy)
    //         return UnderlineStyle.DottedHeavy;
    //
    //     if (value == UnderlineValues.Dash)
    //         return UnderlineStyle.Dash;
    //
    //     if (value == UnderlineValues.DashedHeavy)
    //         return UnderlineStyle.DashedHeavy;
    //
    //     if (value == UnderlineValues.DashLong)
    //         return UnderlineStyle.DashLong;
    //
    //     if (value == UnderlineValues.DashLongHeavy)
    //         return UnderlineStyle.DashLongHeavy;
    //
    //     if (value == UnderlineValues.DotDash)
    //         return UnderlineStyle.DotDash;
    //
    //     if (value == UnderlineValues.DashDotHeavy)
    //         return UnderlineStyle.DashDotHeavy;
    //
    //     if (value == UnderlineValues.DotDotDash)
    //         return UnderlineStyle.DotDotDash;
    //
    //     if (value == UnderlineValues.DashDotDotHeavy)
    //         return UnderlineStyle.DashDotDotHeavy;
    //
    //     if (value == UnderlineValues.Wave)
    //         return UnderlineStyle.Wave;
    //
    //     if (value == UnderlineValues.WavyHeavy)
    //         return UnderlineStyle.WavyHeavy;
    //
    //     if (value == UnderlineValues.WavyDouble)
    //         return UnderlineStyle.WavyDouble;
    //
    //     return UnderlineStyle.None;
    // }


    private static string? MapHighlightToHex(HighlightColorValues? value)
    {
        if (value == HighlightColorValues.Black)
            return "#000000";

        if (value == HighlightColorValues.Blue)
            return "#0000FF";

        if (value == HighlightColorValues.Cyan)
            return "#00FFFF";

        if (value == HighlightColorValues.Green)
            return "#00FF00";

        if (value == HighlightColorValues.Magenta)
            return "#FF00FF";

        if (value == HighlightColorValues.Red)
            return "#FF0000";

        if (value == HighlightColorValues.Yellow)
            return "#FFFF00";

        if (value == HighlightColorValues.White)
            return "#FFFFFF";

        if (value == HighlightColorValues.DarkBlue)
            return "#00008B";

        if (value == HighlightColorValues.DarkCyan)
            return "#008B8B";

        if (value == HighlightColorValues.DarkGreen)
            return "#006400";

        if (value == HighlightColorValues.DarkMagenta)
            return "#8B008B";

        if (value == HighlightColorValues.DarkRed)
            return "#8B0000";

        if (value == HighlightColorValues.DarkYellow)
            return "#808000";

        if (value == HighlightColorValues.DarkGray)
            return "#A9A9A9";

        if (value == HighlightColorValues.LightGray)
            return "#D3D3D3";

        return null;
    }

    private static string ApplyTint(string hex, string tintHex)
    {
        if (!byte.TryParse(tintHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var tint))
            return hex;

        var (r, g, b) = ParseHex(hex);

        r = (byte)(r + (255 - r) * tint / 255.0);
        g = (byte)(g + (255 - g) * tint / 255.0);
        b = (byte)(b + (255 - b) * tint / 255.0);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static string ApplyShade(string hex, string shadeHex)
    {
        if (!byte.TryParse(shadeHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var shade))
            return hex;

        var factor = shade / 255.0;
        var (r, g, b) = ParseHex(hex);

        r = (byte)(r * factor);
        g = (byte)(g * factor);
        b = (byte)(b * factor);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static (byte r, byte g, byte b) ParseHex(string hex)
    {
        hex = hex.TrimStart('#');

        return (
            byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
        );
    }
}