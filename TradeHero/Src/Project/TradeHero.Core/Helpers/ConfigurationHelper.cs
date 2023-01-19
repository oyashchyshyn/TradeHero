using System.Reflection;
using Microsoft.Extensions.Configuration;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Settings.AppSettings;

namespace TradeHero.Core.Helpers;

public static class ConfigurationHelper
{
    public static IConfiguration GenerateConfiguration(string[] args)
    {
        var assembly = Assembly.GetAssembly(typeof(AppSettings));
        if (assembly == null)
        {
            throw new Exception("Cannot load main assembly");
        }
        
        using var stream = assembly.GetManifestResourceStream("TradeHero.Core.app.json");
        if (stream == null)
        {
            throw new Exception("Cannot find app.json");
        }

        return new ConfigurationBuilder()
            .AddJsonStream(stream)
            .AddCommandLine(args)
            .Build();
    }

    public static IConfiguration SetEnvironmentProperties(IConfiguration configuration, string basePath, 
        RunnerType runnerType, EnvironmentType environmentType)
    {
        configuration[EnvironmentConstants.BasePath] = basePath;
        configuration[EnvironmentConstants.RunnerType] = runnerType.ToString();
        configuration[EnvironmentConstants.EnvironmentType] = environmentType.ToString();

        return configuration;
    }
    
    public static AppSettings ConvertConfigurationToAppSettings(IConfiguration configuration)
    {
        return configuration.Get<AppSettings>() ?? new AppSettings();
    }
}