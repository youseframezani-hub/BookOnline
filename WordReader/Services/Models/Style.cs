using System;
using System.Collections.Generic;

namespace WordReader.Services.Models;
public class Style
{
    public string Name { get; set; } = string.Empty;      // display name / unique key
    public string? StyleId { get; set; }                  // Word internal style id
    public StyleType Type { get; set; } = StyleType.Paragraph;
    public string? BasedOnStyleId { get; set; }
    public string? NextParagraphStyleId { get; set; }
    public bool IsCustom { get; set; }

    public FontStyleInfo Font { get; set; } = new();
    public ParagraphStyleInfo Paragraph { get; set; } = new();
    public BorderStyleInfo Border { get; set; } = new();
}

public enum StyleType
{
    Paragraph,
    Character,
    Table,
    Numbering
}

public record BorderStyleInfo
{
    public BorderLineStyle TopStyle { get; set; } = BorderLineStyle.None;
    public BorderLineStyle BottomStyle { get; set; } = BorderLineStyle.None;
    public BorderLineStyle LeftStyle { get; set; } = BorderLineStyle.None;
    public BorderLineStyle RightStyle { get; set; } = BorderLineStyle.None;

    public double? TopWidthPt { get; set; }
    public double? BottomWidthPt { get; set; }
    public double? LeftWidthPt { get; set; }
    public double? RightWidthPt { get; set; }

    public string? TopColor { get; set; }
    public string? BottomColor { get; set; }
    public string? LeftColor { get; set; }
    public string? RightColor { get; set; }
}

public record ParagraphStyleInfo
{
    public ParagraphAlignment? Alignment { get; set; }
    public OutlineLevel? OutlineLevel { get; set; }
    public TextDirection? Direction { get; set; }

    public double? LeftIndentPt { get; set; }
    public double? RightIndentPt { get; set; }
    public double? FirstLineIndentPt { get; set; }
    public double? HangingIndentPt { get; set; }

    public double? SpacingBeforePt { get; set; }
    public double? SpacingAfterPt { get; set; }

    public LineSpacingType? LineSpacingType { get; set; }
    public double? LineSpacing { get; set; }

    public bool? KeepNext { get; set; }
    public bool? KeepLinesTogether { get; set; }
    public bool? PageBreakBefore { get; set; }
    public bool? WidowControl { get; set; }
}

public record FontStyleInfo
{
    public string? LatinFontName { get; set; }
    public string? ComplexScriptFontName { get; set; }
    public string? EastAsiaFontName { get; set; }

    public double? SizePt { get; set; }
    public double? ComplexScriptSizePt { get; set; }

    public bool? Bold { get; set; }
    public bool? Italic { get; set; }
    public bool? BoldComplexScript { get; set; }
    public bool? ItalicComplexScript { get; set; }

    public UnderlineStyle UnderlineStyle { get; set; } = UnderlineStyle.None;

    public bool? Strike { get; set; }
    public bool? DoubleStrike { get; set; }
    public bool? SmallCaps { get; set; }
    public bool? AllCaps { get; set; }
    public bool? Hidden { get; set; }
    public bool? Superscript { get; set; }
    public bool? Subscript { get; set; }

    public string? Color { get; set; }              // #RRGGBB
    public int? ColorRgb { get; set; }              // 0xRRGGBB
    public string? HighlightColor { get; set; }     // #RRGGBB if resolvable
    public string? UnderlineColor { get; set; }     // #RRGGBB
}

public enum ParagraphAlignment
{
    Left,
    Center,
    Right,
    Justify,
    Distributed,
    Both
}

public enum OutlineLevel
{
    BodyText,
    Level1,
    Level2,
    Level3,
    Level4,
    Level5,
    Level6,
    Level7,
    Level8,
    Level9
}

public enum TextDirection
{
    LeftToRight,
    RightToLeft
}

public enum LineSpacingType
{
    Auto,
    AtLeast,
    Exactly
}

public enum UnderlineStyle
{
    None,
    Single,
    Words,
    Double,
    Thick,
    Dotted,
    DottedHeavy,
    Dash,
    DashedHeavy,
    DashLong,
    DashLongHeavy,
    DotDash,
    DashDotHeavy,
    DotDotDash,
    DashDotDotHeavy,
    Wave,
    WavyHeavy,
    WavyDouble
}

public enum BorderLineStyle
{
    None,
    Single,
    Double,
    Dotted,
    Dashed,
    DashSmallGap,
    DotDash,
    DotDotDash,
    Triple,
    ThinThickSmallGap,
    ThickThinSmallGap,
    ThinThickThinSmallGap,
    Wave
}
