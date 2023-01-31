using System.Reflection;
using Newtonsoft.Json;
using TradeHero.Core.Constants;
using TradeHero.Core.Contracts.Settings;
using TradeHero.Core.Enums;

namespace TradeHero.Core.Helpers;

public class AppSettingsHelper
{
    public static AppSettings GenerateAppSettings(string basePath, EnvironmentType environmentType, RunnerType runnerType)
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

        AppSettings? appSettings;
        using (var sr = new StreamReader(stream))
        using (var jsonTextReader = new JsonTextReader(sr))
        {
            var serializer = new JsonSerializer();
            appSettings = serializer.Deserialize<AppSettings>(jsonTextReader);
        }

        if (appSettings == null)
        {
            throw new Exception("Cannot generate settings from app.json");
        }
        
        appSettings.CustomValues.Add(EnvironmentConstants.BasePath, basePath);
        appSettings.CustomValues.Add(EnvironmentConstants.RunnerType, runnerType.ToString());
        appSettings.CustomValues.Add(EnvironmentConstants.EnvironmentType, environmentType.ToString());

        return appSettings;
    }
}