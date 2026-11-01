using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace WordReader.Services.Models;

public class BookStyleEntity
{
    public string Name { get; set; } = string.Empty; // display name / unique key
    public string? StyleId { get; set; } // Word internal style id
    public StyleType? Type { get; set; } = StyleType.Paragraph;
    public string? BasedOnStyleId { get; set; }
    public int? StyleParagraphPropertiesId { get; set; }
    public int? StyleRunPropertiesId { get; set; }
    [NotMapped] public StyleParagraphPropertiesEntity? StyleParagraphProperties { get; set; }
    [NotMapped] public StyleRunPropertiesEntity? StyleRunProperties { get; set; }
    public StyleTablePropertiesEntity? StyleTableProperties { get; set; }
}


public class StyleRunPropertiesEntity
{
    public int Id { get; set; }
    public RunFontsModel? RunFonts { get; set; }
    public bool? Bold { get; set; }
    public bool? Italic { get; set; }
    public bool? Caps { get; set; }
    public bool? SmallCaps { get; set; }
    public bool? Strike { get; set; }
    public bool? DoubleStrike { get; set; }
    public bool? Outline { get; set; }
    public bool? Shadow { get; set; }
    public bool? Emboss { get; set; }
    public bool? Imprint { get; set; }
    public bool? NoProof { get; set; }
    public bool? SnapToGrid { get; set; }
    public bool? Vanish { get; set; }
    public bool? WebHidden { get; set; }
    public ColorModel? Color { get; set; }
    public int? Spacing { get; set; }
    public long? CharacterScale { get; set; }
    public uint? Kern { get; set; }
    public string? Position { get; set; }
    public string? FontSize { get; set; }
    public UnderlineModel? Underline { get; set; }
    public TextEffectKind? TextEffect { get; set; }
    public BorderModel? Border { get; set; }
    public ShadingModel? Shading { get; set; }
    public FitTextModel? FitText { get; set; }
    public VerticalPositionKind? VerticalTextAlignment { get; set; }
    public EmphasisMarkKind? Emphasis { get; set; }
    public LanguageModel? Languages { get; set; }
    public EastAsianLayoutModel? EastAsianLayout { get; set; }
    public bool? SpecVanish { get; set; }
    public string? FontSizeComplexScript { get; set; }
}

public class EastAsianLayoutModel
{
    public int? Id { get; set; }
    public bool? Combine { get; set; }
    public CombineBracketKind? CombineBrackets { get; set; }
}

public enum CombineBracketKind
{
    None,
    Round,
    Square,
    Angle,
    Curly
}

public class LanguageModel
{
    public string? Val { get; set; }
    public string? Bidi { get; set; }
    public string? EastAsia { get; set; }
}

public enum EmphasisMarkKind
{
    None,
    Dot,
    Comma,
    Circle,
    Underdot
}

public enum VerticalPositionKind
{
    Baseline,
    Superscript,
    Subscript
}

public class FitTextModel
{
    public uint? Val { get; set; }
    public int? Id { get; set; }
}

public class BorderModel
{
    public BorderKind? Val { get; set; }
    public string? Color { get; set; }
    public ThemeColorKind? ThemeColor { get; set; }
    public string? ThemeTint { get; set; }
    public string? ThemeShade { get; set; }
    public uint? Size { get; set; }
    public uint? Space { get; set; }
    public bool? Shadow { get; set; }
    public bool? Frame { get; set; }
}

public enum BorderKind
{
    Nil,
    None,
    Single,
    Thick,
    Double,
    Dotted,
    Dashed,
    DotDash,
    DotDotDash,
    Triple,
    ThinThickSmallGap,
    ThickThinSmallGap,
    ThinThickThinSmallGap,
    ThinThickMediumGap,
    ThickThinMediumGap,
    ThinThickThinMediumGap,
    ThinThickLargeGap,
    ThickThinLargeGap,
    ThinThickThinLargeGap,
    Wave,
    DoubleWave,
    DashSmallGap,
    DashDotStroked,
    ThreeDEmboss,
    ThreeDEngrave,
    Outset,
    Inset,
    Apples,
    ArchedScallops,
    BabyPacifier,
    BabyRattle,
    Balloons3Colors,
    BalloonsHotAir,
    BasicBlackDashes,
    BasicBlackDots,
    BasicBlackSquares,
    BasicThinLines,
    BasicWhiteDashes,
    BasicWhiteDots,
    BasicWhiteSquares,
    BasicWideInline,
    BasicWideMidline,
    BasicWideOutline,
    Bats,
    Birds,
    BirdsFlight,
    Cabins,
    CakeSlice,
    CandyCorn,
    CelticKnotwork,
    CertificateBanner,
    ChainLink,
    ChampagneBottle,
    CheckedBarBlack,
    CheckedBarColor,
    Checkered,
    ChristmasTree,
    CirclesLines,
    CirclesRectangles,
    ClassicalWave,
    Clocks,
    Compass,
    Confetti,
    ConfettiGrays,
    ConfettiOutline,
    ConfettiStreamers,
    ConfettiWhite,
    CornerTriangles,
    CouponCutoutDashes,
    CouponCutoutDots,
    CrazyMaze,
    CreaturesButterfly,
    CreaturesFish,
    CreaturesInsects,
    CreaturesLadyBug,
    CrossStitch,
    Cup,
    DecoArch,
    DecoArchColor,
    DecoBlocks,
    DiamondsGray,
    DoubleD,
    DoubleDiamonds,
    Earth1,
    Earth2,
    EclipsingSquares1,
    EclipsingSquares2,
    EggsBlack,
    Fans,
    Film,
    Firecrackers,
    FlowersBlockPrint,
    FlowersDaisies,
    FlowersModern1,
    FlowersModern2,
    FlowersPansy,
    FlowersRedRose,
    FlowersRoses,
    FlowersTeacup,
    FlowersTiny,
    Gems,
    GingerbreadMan,
    Gradient,
    Handmade1,
    Handmade2,
    HeartBalloon,
    HeartGray,
    Hearts,
    HeebieJeebies,
    Holly,
    HouseFunky,
    Hypnotic,
    IceCreamCones,
    LightBulb,
    Lightning1,
    Lightning2,
    MapPins,
    MapleLeaf,
    MapleMuffins,
    Marquee,
    MarqueeToothed,
    Moons,
    Mosaic,
    MusicNotes,
    Northwest,
    Ovals,
    Packages,
    PalmsBlack,
    PalmsColor,
    PaperClips,
    Papyrus,
    PartyFavor,
    PartyGlass,
    Pencils,
    People,
    PeopleWaving,
    PeopleHats,
    Poinsettias,
    PostageStamp,
    Pumpkin1,
    PushPinNote2,
    PushPinNote1,
    Pyramids,
    PyramidsAbove,
    Quadrants,
    Rings,
    Safari,
    Sawtooth,
    SawtoothGray,
    ScaredCat,
    Seattle,
    ShadowedSquares,
    SharksTeeth,
    ShorebirdTracks,
    Skyrocket,
    SnowflakeFancy,
    Snowflakes,
    Sombrero,
    Southwest,
    Stars,
    StarsTop,
    Stars3d,
    StarsBlack,
    StarsShadowed,
    Sun,
    Swirligig,
    TornPaper,
    TornPaperBlack,
    Trees,
    TriangleParty,
    Triangles,
    Tribal1,
    Tribal2,
    Tribal3,
    Tribal4,
    Tribal5,
    Tribal6,
    Triangle1,
    Triangle2,
    TriangleCircle1,
    TriangleCircle2,
    Shapes1,
    Shapes2,
    TwistedLines1,
    TwistedLines2,
    Vine,
    Waveline,
    WeavingAngles,
    WeavingBraid,
    WeavingRibbon,
    WeavingStrips,
    WhiteFlowers,
    Woodwork,
    XIllusions,
    ZanyTriangles,
    ZigZag,
    ZigZagStitch
}

public class UnderlineModel
{
    public UnderlineKind? Val { get; set; }
    public string? Color { get; set; }
    public ThemeColorKind? ThemeColor { get; set; }
    public string? ThemeTint { get; set; }
    public string? ThemeShade { get; set; }
}

public enum TextEffectKind
{
    BlinkBackground,
    Lights,
    AntsBlack,
    AntsRed,
    Shimmer,
    Sparkle,
    None
}

public enum UnderlineKind
{
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
    DotDotDash,
    DashDotDotHeavy,
    Wave,
    WavyHeavy,
    WavyDouble,
    None
}

public class ColorModel
{
    public string? Val { get; set; }
    public ThemeColorKind? ThemeColor { get; set; }
    public string? ThemeTint { get; set; }
    public string? ThemeShade { get; set; }
}

public class RunFontsModel
{
    public FontTypeHintKind? Hint { get; set; }
    public string? Ascii { get; set; }
    public string? HighAnsi { get; set; }
    public string? EastAsia { get; set; }
    public string? ComplexScript { get; set; }
    public ThemeFontKind? AsciiTheme { get; set; }
    public ThemeFontKind? HighAnsiTheme { get; set; }
    public ThemeFontKind? EastAsiaTheme { get; set; }
    public ThemeFontKind? ComplexScriptTheme { get; set; }
}

public enum ThemeFontKind
{
    [EnumMember(Value = "majorEastAsia")] MajorEastAsia,
    [EnumMember(Value = "majorBidi")] MajorBidi,
    [EnumMember(Value = "majorAscii")] MajorAscii,
    [EnumMember(Value = "majorHAnsi")] MajorHAnsi,
    [EnumMember(Value = "minorEastAsia")] MinorEastAsia,
    [EnumMember(Value = "minorAscii")] MinorAscii,
    [EnumMember(Value = "minorBidi")] MinorBidi,
    [EnumMember(Value = "minorHAnsi")] MinorHAnsi
}

public enum FontTypeHintKind
{
    Default,
    EastAsia,
    Cs
}

public enum StyleType
{
    Paragraph,
    Character,
    Table,
    Numbering
}

/// <summary>
/// DTO representation of WordprocessingML paragraph properties used in styles.
/// This is a renderer/database model, not an Open XML SDK replacement.
/// Null means the property was not explicitly present in this XML element.
/// </summary>
public class StyleParagraphPropertiesEntity
{
    public int Id { get; set; }
    public bool? KeepNext { get; set; }
    public bool? KeepLines { get; set; }
    public bool? PageBreakBefore { get; set; }
    public FramePropertiesModel? FrameProperties { get; set; }

    public bool? WidowControl { get; set; }

    // public NumberingPropertiesModel? NumberingProperties { get; set; }
    public bool? SuppressLineNumbers { get; set; }
    public ParagraphBordersModel? ParagraphBorders { get; set; }
    public ShadingModel? Shading { get; set; }
    public List<TabStopModel>? Tabs { get; set; }
    public bool? SuppressAutoHyphens { get; set; }
    public bool? Kinsoku { get; set; }
    public bool? WordWrap { get; set; }
    public bool? OverflowPunctuation { get; set; }
    public bool? TopLinePunctuation { get; set; }
    public bool? AutoSpaceDE { get; set; }
    public bool? AutoSpaceDN { get; set; }
    public bool? BiDi { get; set; }
    public bool? AdjustRightIndent { get; set; }
    public bool? SnapToGrid { get; set; }
    public SpacingBetweenLinesModel? Spacing { get; set; }
    public IndentationModel? Indentation { get; set; }
    public bool? ContextualSpacing { get; set; }
    public bool? MirrorIndents { get; set; }
    public bool? SuppressOverlap { get; set; }
    public JustificationKind? Justification { get; set; }
    public TextDirectionKind? TextDirection { get; set; }
    public TextAlignmentKind? TextAlignment { get; set; }
    public TextBoxTightWrapKind? TextBoxTightWrap { get; set; }

    public OutlineLevelModel? OutlineLevel { get; set; }
    // public ParagraphPropertiesChangeModel? ParagraphPropertiesChange { get; set; }
}

public sealed class IndentationModel
{
    public double? Left { get; set; }
    public double? Right { get; set; }
    public double? Start { get; set; }
    public double? End { get; set; }
    public double? FirstLine { get; set; }
    public double? Hanging { get; set; }
    public double? LeftChars { get; set; }
    public double? RightChars { get; set; }
    public double? FirstLineChars { get; set; }
    public double? HangingChars { get; set; }
}

public sealed class SpacingBetweenLinesModel
{
    public double? Before { get; set; }
    public double? After { get; set; }
    public double? Line { get; set; }
    public LineSpacingRuleKind? LineRule { get; set; }
    public double? BeforeLines { get; set; }
    public double? AfterLines { get; set; }
    public bool? BeforeAutoSpacing { get; set; }
    public bool? AfterAutoSpacing { get; set; }
}

public enum LineSpacingRuleKind
{
    Auto,
    Exact,
    AtLeast
}

public enum JustificationKind
{
    Start,
    Center,
    End,
    Both,
    Distribute,
    Left,
    Right,
    MediumKashida,
    HighKashida,
    LowKashida,
    ThaiDistribute
}

public enum TextDirectionKind
{
    RightToLeft,
    LeftToRight
}

public enum TextAlignmentKind
{
    Top,
    Center,
    Baseline,
    Bottom,
    Auto
}

public enum TextBoxTightWrapKind
{
    None,
    AllLines,
    FirstAndLastLine,
    FirstLineOnly,
    LastLineOnly
}

public enum OutlineLevelModel
{
    Level1,
    Level2,
    Level3,
    Level4,
    Level5,
    Level6,
    Level7,
    Level8,
    Level9,
    BodyText
}

public sealed class NumberingPropertiesModel
{
    public int? Level { get; set; }
    public string? NumberingId { get; set; }
}

public sealed class TabStopModel
{
    /// <summary>Examples: clear, start, center, end, decimal, bar, num.</summary>
    public TabAlignmentKind? Alignment { get; set; }

    /// <summary>Examples: dot, hyphen, underscore, heavy, middleDot, none.</summary>
    public TabLeaderKind? Leader { get; set; }

    /// <summary>Word tab position in point, as encoded in XML.</summary>
    public double? Position { get; set; }
}

public enum TabAlignmentKind
{
    Clear,
    Start,
    Center,
    End,
    Decimal,
    Bar,
    Number,
    Left,
    Right
}

public enum TabLeaderKind
{
    None,
    Dot,
    Hyphen,
    Underscore,
    Heavy,
    MiddleDot
}

public sealed class FramePropertiesModel
{
    public DropCapLocationKind? DropCap { get; set; }
    public int? Lines { get; set; }
    public string? Width { get; set; }
    public uint? Height { get; set; }
    public string? VerticalSpace { get; set; }
    public string? HorizontalSpace { get; set; }
    public TextWrappingKind? Wrap { get; set; }
    public HorizontalAnchorKind? HorizontalPosition { get; set; }
    public VerticalAnchorKind? VerticalPosition { get; set; }
    public string? X { get; set; }
    public HorizontalAlignmentKind? XAlign { get; set; }
    public string? Y { get; set; }
    public VerticalAlignmentKind? YAlign { get; set; }
    public HeightRuleKind? HeightType { get; set; }
    public bool? AnchorLock { get; set; }
}

public enum HeightRuleKind
{
    Auto,
    Exact,
    AtLeast
}

public enum VerticalAlignmentKind
{
    Inline,
    Top,
    Center,
    Bottom,
    Inside,
    Outside
}

public enum HorizontalAlignmentKind
{
    Left,
    Center,
    Right,
    Inside,
    Outside
}

public enum VerticalAnchorKind
{
    Text,
    Margin,
    Page
}

public enum HorizontalAnchorKind
{
    Text,
    Margin,
    Page
}

public enum TextWrappingKind
{
    Auto,
    NotBeside,
    Around,
    Tight,
    Through,
    None
}

public enum DropCapLocationKind
{
    None,
    Drop,
    Margin
}

public sealed class ParagraphBordersModel
{
    public BorderModel? Top { get; set; }
    public BorderModel? Left { get; set; }
    public BorderModel? Bottom { get; set; }
    public BorderModel? Right { get; set; }
    public BorderModel? Between { get; set; }
    public BorderModel? Bar { get; set; }
}

public enum ThemeColorKind
{
    Dark1,
    Light1,
    Dark2,
    Light2,
    Accent1,
    Accent2,
    Accent3,
    Accent4,
    Accent5,
    Accent6,
    Hyperlink,
    FollowedHyperlink,
    None,
    Background1,
    Text1,
    Background2,
    Text2
}

public sealed class ShadingModel
{
    public ShadingPatternKind? Val { get; set; }
    public string? Color { get; set; }
    public ThemeColorKind? ThemeColor { get; set; }
    public string? ThemeTint { get; set; }
    public string? ThemeShade { get; set; }
    public string? Fill { get; set; }
    public ThemeColorKind? ThemeFill { get; set; }
    public string? ThemeFillTint { get; set; }
    public string? ThemeFillShade { get; set; }
}

public enum ShadingPatternKind
{
    Nil,
    Clear,
    Solid,
    HorizontalStripe,
    VerticalStripe,
    ReverseDiagonalStripe,
    DiagonalStripe,
    HorizontalCross,
    DiagonalCross,
    ThinHorizontalStripe,
    ThinVerticalStripe,
    ThinReverseDiagonalStripe,
    ThinDiagonalStripe,
    ThinHorizontalCross,
    ThinDiagonalCross,
    Percent5,
    Percent10,
    Percent12,
    Percent15,
    Percent20,
    Percent25,
    Percent30,
    Percent35,
    Percent37,
    Percent40,
    Percent45,
    Percent50,
    Percent55,
    Percent60,
    Percent65,
    Percent70,
    Percent75,
    Percent80,
    Percent85,
    Percent87,
    Percent90,
    Percent95,
    Percent62
}

public sealed class ParagraphPropertiesChangeModel
{
    public string? Author { get; set; }
    public DateTimeOffset? Date { get; set; }
    public string? Id { get; set; }
    public StyleParagraphPropertiesEntity? PreviousProperties { get; set; }
}