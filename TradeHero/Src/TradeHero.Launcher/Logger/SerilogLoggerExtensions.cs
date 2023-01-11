using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using TradeHero.Core.Enums;
using TradeHero.Launcher.Services;

namespace TradeHero.Launcher.Logger;

public static class SerilogLoggerExtensions
{
    public static void AddSerilog(this ILoggingBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof (builder));   
        }
        
        builder.Services.AddSingleton<ILoggerProvider, SerilogLoggerProvider>(serviceProvider =>
        {
            var environmentService = serviceProvider.GetRequiredService<EnvironmentService>();
            var appSettings = environmentService.GetAppSettings();
            
            LoggerConfiguration loggerConfiguration;

            if (appSettings.Logger.LogLevel != LogLevel.None)
            {
                var loggerFilePath = Path.Combine(environmentService.GetBasePath(),
                    appSettings.Folder.DataFolderName, appSettings.Folder.LogsFolderName,
                    appSettings.Logger.AppFileName);
                
                loggerConfiguration = new LoggerConfiguration()
                    .MinimumLevel.Is((LogEventLevel)appSettings.Logger.LogLevel)
                    .Enrich.FromLogContext()
                    .WriteTo.File
                    (
                        loggerFilePath,
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: appSettings.Logger.LogTemplate,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 209715200 // Limit file size 200mb.
                    );

                if (environmentService.GetEnvironmentType() == EnvironmentType.Development)
                {
                    loggerConfiguration.WriteTo.Console(outputTemplate: appSettings.Logger.LogTemplate, 
                        theme: AnsiConsoleTheme.Code);
                }
            }
            else
            {
                loggerConfiguration = new LoggerConfiguration();
            }

            return new SerilogLoggerProvider(loggerConfiguration.CreateLogger(), true);
        });
        
        builder.AddFilter<SerilogLoggerProvider>(null, LogLevel.Trace);
    }
}