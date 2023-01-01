using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Runner.Helpers;

internal static class ArgumentsHelper
{
    public static EnvironmentType GetEnvironmentType(string[] args)
    {
        var environmentType = EnvironmentType.Production;

        if (!args.Any(x => x.StartsWith(ArgumentConstants.EnvironmentKey)))
        {
            return environmentType;
        }
        
        var argValue = args
            .First(x => x.StartsWith(ArgumentConstants.EnvironmentKey))
            .Replace(ArgumentConstants.EnvironmentKey, string.Empty);

        environmentType = (EnvironmentType)Enum.Parse(typeof(EnvironmentType), argValue);

        return environmentType;
    }
}