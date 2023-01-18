using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using TradeHero.Core.Enums;
using TradeHero.Core.Settings.AppSettings;

namespace TradeHero.LauncherLightWeight.Helpers;

internal static class LoggerGenerator
{
    public static ILoggerFactory GenerateLoggerFactory(string basePath, AppSettings appSettings, 
        EnvironmentType environmentType, RunnerType runnerType)
    {
        LoggerConfiguration loggerConfiguration;

        if (appSettings.Logger.LogLevel != LogLevel.None)
        {
            var logInstance = runnerType switch
            {
                RunnerType.Launcher => appSettings.Logger.LauncherInstance,
                RunnerType.App => appSettings.Logger.AppInstance,
                _ => throw new ArgumentOutOfRangeException(nameof(runnerType), runnerType, null)
            };

            var loggerFilePath = Path.Combine(basePath,
                appSettings.Folder.LogsFolderName, logInstance.FileName);

            loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is((LogEventLevel)appSettings.Logger.LogLevel)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File
                (
                    loggerFilePath,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: logInstance.LogTemplate,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 209715200 // Limit file size 200mb.
                );

            if (environmentType == EnvironmentType.Development)
            {
                loggerConfiguration.WriteTo.Console(outputTemplate: logInstance.LogTemplate,
                    theme: AnsiConsoleTheme.Code);
            }
        }
        else
        {
            loggerConfiguration = new LoggerConfiguration();
        }

        return new SerilogLoggerFactory(loggerConfiguration.CreateLogger(), true);
    }
}