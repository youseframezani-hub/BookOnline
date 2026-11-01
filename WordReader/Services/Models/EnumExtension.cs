using DocumentFormat.OpenXml;

namespace WordReader.Services.Models;

public static class EnumExtension
{
    public static TTarget MapEnum<TSource, TTarget>(this TSource source)
        where TSource : struct, IEnumValue
        where TTarget : struct, Enum

    {
        var result = Enum.Parse<TTarget>(source.Value, true);
        return result;
    }
}