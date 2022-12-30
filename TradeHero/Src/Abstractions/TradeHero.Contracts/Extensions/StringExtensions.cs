using System.Globalization;

namespace TradeHero.Contracts.Extensions;

public static class StringExtensions
{
    public static string CapitalizeFirstLetter(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return input[..1].ToUpper(CultureInfo.CurrentCulture) +
               input.Substring(1, input.Length - 1);
    }
    
    public static string LowercaseFirstLetter(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return input[..1].ToLower(CultureInfo.CurrentCulture) +
               input.Substring(1, input.Length - 1);
    }
}