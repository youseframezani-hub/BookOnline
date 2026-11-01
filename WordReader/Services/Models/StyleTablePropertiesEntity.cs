
namespace WordReader.Services.Models;

public class StyleTablePropertiesEntity
{
    public int Id { get; set; }
    public int? TableStyleRowBandSize { get; set; }
    public int? TableStyleColumnBandSize { get; set; }
    public TableRowAlignmentKind? TableJustification { get; set; }
    public TableIndentationModel? TableIndentation { get; set; }
    public TableBordersModel? TableBorders { get; set; }
    public TableCellSpacingModel? TableCellSpacing { get; set; }
    public ShadingModel? Shading { get; set; }
    public TableCellMarginDefaultModel? TableCellMarginDefault { get; set; }
    /*
     *
     * TableCellMarginDefault ->
     *          TopMargin : TableWidthType -> Width(int)-Type(TableWidthUnitValues:nil,dxa,pct)
     *          TableCellLeftMargin -> TableWidthDxaNilType
     *          StartMargin : TableWidthType
                BottomMargin :TableWidthType
                TableCellRightMargin -> TableWidthDxaNilType
                EndMargin : TableWidthType
     */
}

public class TableCellMarginDefaultModel
{
    public TableCellSpacingModel? TopMargin { get; set; }
    public TableCellSpacingModel? StartMargin { get; set; }
    public TableCellSpacingModel? BottomMargin { get; set; }
    public TableCellSpacingModel? EndMargin { get; set; }
    public TableWidthDxaNilTypeModel? TableCellLeftMarginModel { get; set; }
    public TableWidthDxaNilTypeModel? TableCellRightMarginModel { get; set; }


}


public class TableWidthDxaNilTypeModel
{
    public short? Width { get; set; }
    public TableWidthKind? Type { get; set; }
}

public enum TableWidthKind
{
    Nil,Dxa
}
public class TableCellSpacingModel
{
    public string? Width { get; set; }
    public TableWidthUnitKind? Type { get; set; }
}

public enum TableWidthUnitKind
{
    Nil,
    Pct,
    Dxa,
    Auto
}

public class TableBordersModel
{
    public BorderModel? TopBorder { get; set; }
    public BorderModel? LeftBorder { get; set; }
    public BorderModel? StartBorder { get; set; }
    public BorderModel? BottomBorder { get; set; }
    public BorderModel? RightBorder { get; set; }
    public BorderModel? EndBorder { get; set; }
    public BorderModel? InsideHorizontalBorder { get; set; } 
    public BorderModel? InsideVerticalBorder { get; set; }
}

public class TableIndentationModel
{
    public int? Width { get; set; }
    public TableWidthUnitKind? Type { get; set; }
}


public class TableStyleRowBandSizeModel
{
    public int? Val { get; set; }
}

public class TableStyleColumnBandSizeModel
{
    public int? Val { get; set; }
}

public enum TableRowAlignmentKind
{
    Left,
    Center,
    Right,
}