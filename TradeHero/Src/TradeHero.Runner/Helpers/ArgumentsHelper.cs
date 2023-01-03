using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Runner.Helpers;

internal static class ArgumentsHelper
{
    public static EnvironmentType GetEnvironmentType(string[] args)
    {
        const string environmentArgumentKey = $"--{ArgumentConstants.EnvironmentKey}=";
        
        if (!args.Any(x => x.StartsWith(environmentArgumentKey)))
        {
            return EnvironmentType.Production;
        }
        
        var argValue = args
            .First(x => x.StartsWith(environmentArgumentKey))
            .Replace(environmentArgumentKey, string.Empty);

        return (EnvironmentType)Enum.Parse(typeof(EnvironmentType), argValue);
    }
}