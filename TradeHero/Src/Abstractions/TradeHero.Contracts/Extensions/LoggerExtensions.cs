using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using TradeHero.Contracts.Logger;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Extensions;

public static class LoggerExtensions
{
    public static void AddThSerilog(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, SerilogLoggerProvider>(serviceProvider =>
        {
            var store = serviceProvider.GetRequiredService<IStoreService>();
            var environmentService = serviceProvider.GetRequiredService<IEnvironmentService>();
            var appSettings = environmentService.GetAppSettings();
            
            LoggerConfiguration loggerConfiguration;

            if (appSettings.Logger.LogLevel != LogLevel.None)
            {
                var loggerFilePath = Path.Combine(environmentService.GetBasePath(),
                    appSettings.Folder.LogsFolderName,
                    appSettings.Logger.AppFileName);
                
                loggerConfiguration = new LoggerConfiguration()
                    .MinimumLevel.Is((LogEventLevel)appSettings.Logger.LogLevel)
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning) 
                    .Enrich.FromLogContext()
                    .WriteTo.Sink(new StoreEventSink(store))
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