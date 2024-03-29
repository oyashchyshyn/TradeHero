using TradeHero.Core.Constants;
using TradeHero.Core.Enums;

namespace TradeHero.Core.Helpers;

public static class ArgsHelper
{
    public static EnvironmentType GetEnvironmentType(string[] args)
    {
        if (!args.Any(x => x.StartsWith(ArgumentKeyConstants.Environment)))
        {
            return EnvironmentType.Production;
        }
        
        var argValue = args
            .First(x => x.StartsWith(ArgumentKeyConstants.Environment))
            .Replace(ArgumentKeyConstants.Environment, string.Empty);

        return (EnvironmentType)Enum.Parse(typeof(EnvironmentType), argValue);
    }
    
    public static void IsRunAppKeyExist(IEnumerable<string> args, string runAppKey)
    {
        if (args.All(x => x != runAppKey))
        {
            throw new Exception("Run app key does not exist");   
        }
    }
}