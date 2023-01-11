namespace TradeHero.Core.Extensions;

public static class DecimalExtensions
{
    public static string ToReadable(this decimal value)
    {
        return value.ToString(value < 1 ? "0.##########" : "#.##########");
    }
}