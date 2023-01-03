using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace TradeHero.Runner.Helpers;

internal static class ConfigurationHelper
{
    public static IConfiguration GenerateConfiguration(string[] args)
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly == null)
        {
            throw new Exception("Cannot load main assembly");
        }
        
        using var stream = assembly.GetManifestResourceStream("TradeHero.Runner.app.json");
        if (stream == null)
        {
            throw new Exception("Cannot find app.json");
        }

        return new ConfigurationBuilder()
            .AddJsonStream(stream)
            .AddCommandLine(args)
            .Build();
    }
}