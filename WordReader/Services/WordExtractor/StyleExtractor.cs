
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WordReader.Services.Models;
using A = DocumentFormat.OpenXml.Drawing;
using MyStyle = WordReader.Services.Models;
using OutlineLevel = WordReader.Services.Models.OutlineLevel;


namespace WordReader.Services.WordExtractor;

public interface IExtractStyleService
{
    Dictionary<string, MyStyle.Style> Extract(string filePath);
}

public class ExtractStyleService : IExtractStyleService
{
    public Dictionary<string, MyStyle.Style> Extract(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        var ext = Path.GetExtension(filePath);
        if (!string.Equals(ext, ".docx", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Accurate style extraction is supported for .docx files.");

        using var doc = WordprocessingDocument.Open(filePath, false);

        var themeColors = ReadThemeColors(doc);
        var stylesPart = doc.MainDocumentPart?.StyleDefinitionsPart;
        var result = new Dictionary<string, MyStyle.Style>(StringComparer.OrdinalIgnoreCase);

        if (stylesPart?.Styles == null)
            return result;

        foreach (var s in stylesPart.Styles.Elements<DocumentFormat.OpenXml.Wordprocessing.Style>())
        {
            var style = ExtractStyle(s, themeColors);
            if (string.IsNullOrWhiteSpace(style.Name))
                continue;

            result[style.Name] = style;
        }

        return ResolveInheritance(result);
    }

    private MyStyle.Style ExtractStyle(DocumentFormat.OpenXml.Wordprocessing.Style s, Dictionary<string, string> themeColors)
    {
        var style = new MyStyle.Style
        {
            StyleId = s.StyleId?.Value,
            Name = s.StyleName?.Val?.Value ?? s.StyleId?.Value ?? "UnnamedStyle",
            Type = MapStyleType(s.Type?.Value),
            BasedOnStyleId = s.BasedOn?.Val?.Value,
            NextParagraphStyleId = s.NextParagraphStyle?.Val?.Value,
            IsCustom = s.CustomStyle?.Value ?? false
        };

        if (s.StyleRunProperties != null)
            style.Font = ExtractFont(s.StyleRunProperties, themeColors);

        if (s.StyleParagraphProperties != null)
        {
            style.Paragraph = ExtractParagraph(s.StyleParagraphProperties);
            style.Border = ExtractBorder(s.StyleParagraphProperties);
        }

        return style;
    }

    private FontStyleInfo ExtractFont(StyleRunProperties rp, Dictionary<string, string> themeColors)
    {
        var result = new FontStyleInfo();

        var fonts = rp.RunFonts;
        if (fonts != null)
        {
            result.LatinFontName = fonts.Ascii?.Value;
            result.ComplexScriptFontName = fonts.ComplexScript?.Value;
            result.EastAsiaFontName = fonts.EastAsia?.Value;
        }

        if (rp.FontSize?.Val != null && double.TryParse(rp.FontSize.Val.Value, out var szHalfPt))
            result.SizePt = szHalfPt / 2d;

        if (rp.FontSizeComplexScript?.Val != null && double.TryParse(rp.FontSizeComplexScript.Val.Value, out var csHalfPt))
            result.ComplexScriptSizePt = csHalfPt / 2d;

        result.Bold = rp.Bold != null ? OnOffToBool(rp.Bold.Val) : null;
        result.Italic = rp.Italic != null ? OnOffToBool(rp.Italic.Val) : null;
        result.BoldComplexScript = rp.BoldComplexScript != null ? OnOffToBool(rp.BoldComplexScript.Val) : null;
        result.ItalicComplexScript = rp.ItalicComplexScript != null ? OnOffToBool(rp.ItalicComplexScript.Val) : null;

        result.Strike = rp.Strike != null ? OnOffToBool(rp.Strike.Val) : null;
        result.DoubleStrike = rp.DoubleStrike != null ? OnOffToBool(rp.DoubleStrike.Val) : null;
        result.SmallCaps = rp.SmallCaps != null ? OnOffToBool(rp.SmallCaps.Val) : null;
        result.AllCaps = rp.Caps != null ? OnOffToBool(rp.Caps.Val) : null;
        result.Hidden = rp.Vanish != null ? OnOffToBool(rp.Vanish.Val) : null;

        if (rp.VerticalTextAlignment != null)
        {
            result.Superscript = rp.VerticalTextAlignment.Val == VerticalPositionValues.Superscript;
            result.Subscript = rp.VerticalTextAlignment.Val == VerticalPositionValues.Subscript;
        }

        if (rp.Underline != null)
        {
            result.UnderlineStyle = MapUnderline(rp.Underline.Val?.Value);
            result.UnderlineColor = ResolveColor(
                rp.Underline.Color?.Value,
                rp.Underline.ThemeColor?.Value.ToString(),
                rp.Underline.ThemeTint?.Value,
                rp.Underline.ThemeShade?.Value,
                themeColors);
        }

        if (rp.Color != null)
        {
            var hex = ResolveColor(
                rp.Color.Val?.Value,
                rp.Color.ThemeColor?.Value.ToString(),
                rp.Color.ThemeTint?.Value,
                rp.Color.ThemeShade?.Value,
                themeColors);

            result.Color = hex;
            result.ColorRgb = HexToRgbInt(hex);
        }

        //if (rp.Highlight != null)
        //{
        //    result.HighlightColor = MapHighlightToHex(rp.Highlight.Val?.Value);
        //}

        return result;
    }

    private ParagraphStyleInfo ExtractParagraph(StyleParagraphProperties pp)
    {
        var result = new ParagraphStyleInfo();

        if (pp.Justification != null)
            result.Alignment = MapAlignment(pp.Justification.Val?.Value);

        if (pp.OutlineLevel?.Val != null)
            result.OutlineLevel = MapOutlineLevel(pp.OutlineLevel.Val.Value);

        if (pp.BiDi != null)
            result.Direction = OnOffToBool(pp.BiDi.Val) ? MyStyle.TextDirection.RightToLeft : MyStyle.TextDirection.LeftToRight;

        if (pp.Indentation != null)
        {
            result.LeftIndentPt = TwipToPt(pp.Indentation.Left?.Value);
            result.RightIndentPt = TwipToPt(pp.Indentation.Right?.Value);
            result.FirstLineIndentPt = TwipToPt(pp.Indentation.FirstLine?.Value);
            result.HangingIndentPt = TwipToPt(pp.Indentation.Hanging?.Value);
        }

        if (pp.SpacingBetweenLines != null)
        {
            result.SpacingBeforePt = TwipToPt(pp.SpacingBetweenLines.Before?.Value);
            result.SpacingAfterPt = TwipToPt(pp.SpacingBetweenLines.After?.Value);

            if (pp.SpacingBetweenLines.LineRule != null)
                result.LineSpacingType = MapLineSpacingType(pp.SpacingBetweenLines.LineRule.Value);

            if (pp.SpacingBetweenLines.Line != null)
                result.LineSpacing = LineToDisplayValue(
                    pp.SpacingBetweenLines.Line.Value,
                    pp.SpacingBetweenLines.LineRule?.Value);
        }

        result.KeepNext = pp.KeepNext != null ? OnOffToBool(pp.KeepNext.Val) : null;
        result.KeepLinesTogether = pp.KeepLines != null ? OnOffToBool(pp.KeepLines.Val) : null;
        result.PageBreakBefore = pp.PageBreakBefore != null ? OnOffToBool(pp.PageBreakBefore.Val) : null;
        result.WidowControl = pp.WidowControl != null ? OnOffToBool(pp.WidowControl.Val) : null;

        return result;
    }

    private BorderStyleInfo ExtractBorder(StyleParagraphProperties pp)
    {
        var result = new BorderStyleInfo();
        var borders = pp.ParagraphBorders;
        if (borders == null)
            return result;

        FillBorder(borders.TopBorder, out var topStyle, out var topWidth, out var topColor);
        FillBorder(borders.BottomBorder, out var bottomStyle, out var bottomWidth, out var bottomColor);
        FillBorder(borders.LeftBorder, out var leftStyle, out var leftWidth, out var leftColor);
        FillBorder(borders.RightBorder, out var rightStyle, out var rightWidth, out var rightColor);

        result.TopStyle = topStyle;
        result.TopWidthPt = topWidth;
        result.TopColor = topColor;

        result.BottomStyle = bottomStyle;
        result.BottomWidthPt = bottomWidth;
        result.BottomColor = bottomColor;

        result.LeftStyle = leftStyle;
        result.LeftWidthPt = leftWidth;
        result.LeftColor = leftColor;

        result.RightStyle = rightStyle;
        result.RightWidthPt = rightWidth;
        result.RightColor = rightColor;

        return result;
    }

    private void FillBorder(BorderType border, out BorderLineStyle style, out double? widthPt, out string? color)
    {
        style = BorderLineStyle.None;
        widthPt = null;
        color = null;

        if (border == null)
            return;

        style = MapBorder(border.Val?.Value);

        if (border.Size?.Value != null)
            widthPt = border.Size.Value / 8d;

        color = NormalizeHex(border.Color?.Value);
    }

    private Dictionary<string, MyStyle.Style> ResolveInheritance(Dictionary<string, MyStyle.Style> styles)
    {
        var resolved = new Dictionary<string, MyStyle.Style>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in styles.Values)
            resolved[item.Name] = ResolveStyle(item, styles, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        return resolved;
    }

    private MyStyle.Style ResolveStyle(MyStyle.Style style, Dictionary<string, MyStyle.Style> styles, HashSet<string> chain)
    {
        if (string.IsNullOrWhiteSpace(style.BasedOnStyleId))
            return style;

        if (!chain.Add(style.Name))
            return style;

        var parent = styles.Values.FirstOrDefault(x =>
            string.Equals(x.StyleId, style.BasedOnStyleId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Name, style.BasedOnStyleId, StringComparison.OrdinalIgnoreCase));

        if (parent == null)
            return style;

        parent = ResolveStyle(parent, styles, chain);

        return Merge(parent, style);
    }

    private MyStyle.Style Merge(MyStyle.Style parent, MyStyle.Style child)
    {
        return new MyStyle.Style
        {
            Name = child.Name,
            StyleId = child.StyleId,
            Type = child.Type,
            BasedOnStyleId = child.BasedOnStyleId,
            NextParagraphStyleId = child.NextParagraphStyleId,
            IsCustom = child.IsCustom,

            Font = MergeFont(parent.Font, child.Font),
            Paragraph = MergeParagraph(parent.Paragraph, child.Paragraph),
            Border = MergeBorder(parent.Border, child.Border)
        };
    }

    private FontStyleInfo MergeFont(FontStyleInfo parent, FontStyleInfo child)
    {
        return new FontStyleInfo
        {
            LatinFontName = child.LatinFontName ?? parent.LatinFontName,
            ComplexScriptFontName = child.ComplexScriptFontName ?? parent.ComplexScriptFontName,
            EastAsiaFontName = child.EastAsiaFontName ?? parent.EastAsiaFontName,
            SizePt = child.SizePt ?? parent.SizePt,
            ComplexScriptSizePt = child.ComplexScriptSizePt ?? parent.ComplexScriptSizePt,
            Bold = child.Bold ?? parent.Bold,
            Italic = child.Italic ?? parent.Italic,
            BoldComplexScript = child.BoldComplexScript ?? parent.BoldComplexScript,
            ItalicComplexScript = child.ItalicComplexScript ?? parent.ItalicComplexScript,
            UnderlineStyle = child.UnderlineStyle != UnderlineStyle.None ? child.UnderlineStyle : parent.UnderlineStyle,
            Strike = child.Strike ?? parent.Strike,
            DoubleStrike = child.DoubleStrike ?? parent.DoubleStrike,
            SmallCaps = child.SmallCaps ?? parent.SmallCaps,
            AllCaps = child.AllCaps ?? parent.AllCaps,
            Hidden = child.Hidden ?? parent.Hidden,
            Superscript = child.Superscript ?? parent.Superscript,
            Subscript = child.Subscript ?? parent.Subscript,
            Color = child.Color ?? parent.Color,
            ColorRgb = child.ColorRgb ?? parent.ColorRgb,
            HighlightColor = child.HighlightColor ?? parent.HighlightColor,
            UnderlineColor = child.UnderlineColor ?? parent.UnderlineColor
        };
    }

    private ParagraphStyleInfo MergeParagraph(ParagraphStyleInfo parent, ParagraphStyleInfo child)
    {
        return new ParagraphStyleInfo
        {
            Alignment = child.Alignment ?? parent.Alignment,
            OutlineLevel = child.OutlineLevel ?? parent.OutlineLevel,
            Direction = child.Direction ?? parent.Direction,
            LeftIndentPt = child.LeftIndentPt ?? parent.LeftIndentPt,
            RightIndentPt = child.RightIndentPt ?? parent.RightIndentPt,
            FirstLineIndentPt = child.FirstLineIndentPt ?? parent.FirstLineIndentPt,
            HangingIndentPt = child.HangingIndentPt ?? parent.HangingIndentPt,
            SpacingBeforePt = child.SpacingBeforePt ?? parent.SpacingBeforePt,
            SpacingAfterPt = child.SpacingAfterPt ?? parent.SpacingAfterPt,
            LineSpacingType = child.LineSpacingType ?? parent.LineSpacingType,
            LineSpacing = child.LineSpacing ?? parent.LineSpacing,
            KeepNext = child.KeepNext ?? parent.KeepNext,
            KeepLinesTogether = child.KeepLinesTogether ?? parent.KeepLinesTogether,
            PageBreakBefore = child.PageBreakBefore ?? parent.PageBreakBefore,
            WidowControl = child.WidowControl ?? parent.WidowControl
        };
    }

    private BorderStyleInfo MergeBorder(BorderStyleInfo parent, BorderStyleInfo child)
    {
        return new BorderStyleInfo
        {
            TopStyle = child.TopStyle != BorderLineStyle.None ? child.TopStyle : parent.TopStyle,
            BottomStyle = child.BottomStyle != BorderLineStyle.None ? child.BottomStyle : parent.BottomStyle,
            LeftStyle = child.LeftStyle != BorderLineStyle.None ? child.LeftStyle : parent.LeftStyle,
            RightStyle = child.RightStyle != BorderLineStyle.None ? child.RightStyle : parent.RightStyle,

            TopWidthPt = child.TopWidthPt ?? parent.TopWidthPt,
            BottomWidthPt = child.BottomWidthPt ?? parent.BottomWidthPt,
            LeftWidthPt = child.LeftWidthPt ?? parent.LeftWidthPt,
            RightWidthPt = child.RightWidthPt ?? parent.RightWidthPt,

            TopColor = child.TopColor ?? parent.TopColor,
            BottomColor = child.BottomColor ?? parent.BottomColor,
            LeftColor = child.LeftColor ?? parent.LeftColor,
            RightColor = child.RightColor ?? parent.RightColor
        };
    }

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

    private string? ResolveColor(string? direct, string? themeColor, string? themeTint, string? themeShade, Dictionary<string, string> themeColors)
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
        if (value == null)
            return true;

        return value.Value;
    }

    private static StyleType MapStyleType(StyleValues? value)
    {
        if (value == StyleValues.Character)
            return StyleType.Character;

        if (value == StyleValues.Table)
            return StyleType.Table;

        if (value == StyleValues.Numbering)
            return StyleType.Numbering;

        return StyleType.Paragraph;
    }

    private static ParagraphAlignment? MapAlignment(JustificationValues? value)
    {
        if (value == JustificationValues.Center)
            return ParagraphAlignment.Center;

        if (value == JustificationValues.Right)
            return ParagraphAlignment.Right;

        if (value == JustificationValues.Both)
            return ParagraphAlignment.Justify;

        if (value == JustificationValues.Distribute)
            return ParagraphAlignment.Distributed;

        if (value == JustificationValues.MediumKashida)
            return ParagraphAlignment.Both;

        if (value == JustificationValues.HighKashida)
            return ParagraphAlignment.Both;

        if (value == JustificationValues.LowKashida)
            return ParagraphAlignment.Both;

        return ParagraphAlignment.Left;
    }

    private static OutlineLevel? MapOutlineLevel(int value)
    {
        if (value == 0)
            return OutlineLevel.Level1;

        if (value == 1)
            return OutlineLevel.Level2;

        if (value == 2)
            return OutlineLevel.Level3;

        if (value == 3)
            return OutlineLevel.Level4;

        if (value == 4)
            return OutlineLevel.Level5;

        if (value == 5)
            return OutlineLevel.Level6;

        if (value == 6)
            return OutlineLevel.Level7;

        if (value == 7)
            return OutlineLevel.Level8;

        if (value == 8)
            return OutlineLevel.Level9;

        return OutlineLevel.BodyText;
    }

    private static LineSpacingType? MapLineSpacingType(LineSpacingRuleValues value)
    {
        if (value == LineSpacingRuleValues.AtLeast)
            return LineSpacingType.AtLeast;

        if (value == LineSpacingRuleValues.Exact)
            return LineSpacingType.Exactly;

        return LineSpacingType.Auto;
    }

    private static UnderlineStyle MapUnderline(UnderlineValues? value)
    {
        if (value == UnderlineValues.Single)
            return UnderlineStyle.Single;

        if (value == UnderlineValues.Words)
            return UnderlineStyle.Words;

        if (value == UnderlineValues.Double)
            return UnderlineStyle.Double;

        if (value == UnderlineValues.Thick)
            return UnderlineStyle.Thick;

        if (value == UnderlineValues.Dotted)
            return UnderlineStyle.Dotted;

        if (value == UnderlineValues.DottedHeavy)
            return UnderlineStyle.DottedHeavy;

        if (value == UnderlineValues.Dash)
            return UnderlineStyle.Dash;

        if (value == UnderlineValues.DashedHeavy)
            return UnderlineStyle.DashedHeavy;

        if (value == UnderlineValues.DashLong)
            return UnderlineStyle.DashLong;

        if (value == UnderlineValues.DashLongHeavy)
            return UnderlineStyle.DashLongHeavy;

        if (value == UnderlineValues.DotDash)
            return UnderlineStyle.DotDash;

        if (value == UnderlineValues.DashDotHeavy)
            return UnderlineStyle.DashDotHeavy;

        if (value == UnderlineValues.DotDotDash)
            return UnderlineStyle.DotDotDash;

        if (value == UnderlineValues.DashDotDotHeavy)
            return UnderlineStyle.DashDotDotHeavy;

        if (value == UnderlineValues.Wave)
            return UnderlineStyle.Wave;

        if (value == UnderlineValues.WavyHeavy)
            return UnderlineStyle.WavyHeavy;

        if (value == UnderlineValues.WavyDouble)
            return UnderlineStyle.WavyDouble;

        return UnderlineStyle.None;
    }

    private static BorderLineStyle MapBorder(BorderValues? value)
    {
        if (value == BorderValues.Single)
            return BorderLineStyle.Single;

        if (value == BorderValues.Double)
            return BorderLineStyle.Double;

        if (value == BorderValues.Dotted)
            return BorderLineStyle.Dotted;

        if (value == BorderValues.Dashed)
            return BorderLineStyle.Dashed;

        if (value == BorderValues.DashSmallGap)
            return BorderLineStyle.DashSmallGap;

        if (value == BorderValues.DotDash)
            return BorderLineStyle.DotDash;

        if (value == BorderValues.DotDotDash)
            return BorderLineStyle.DotDotDash;

        if (value == BorderValues.Triple)
            return BorderLineStyle.Triple;

        if (value == BorderValues.ThinThickSmallGap)
            return BorderLineStyle.ThinThickSmallGap;

        if (value == BorderValues.ThickThinSmallGap)
            return BorderLineStyle.ThickThinSmallGap;

        if (value == BorderValues.ThinThickThinSmallGap)
            return BorderLineStyle.ThinThickThinSmallGap;

        if (value == BorderValues.Wave)
            return BorderLineStyle.Wave;

        return BorderLineStyle.None;
    }

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
