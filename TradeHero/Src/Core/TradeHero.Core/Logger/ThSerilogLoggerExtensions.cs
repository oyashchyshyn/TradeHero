using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Settings;
using TradeHero.Contracts.Store;
using SerilogLogger = Serilog.Core.Logger;

namespace TradeHero.Core.Logger;

internal static class ThSerilogLoggerExtensions
{
    public static void AddThSerilog(this ILoggingBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof (builder));   
        }
        
        builder.Services.AddSingleton<ILoggerProvider, SerilogLoggerProvider>(serviceProvider =>
        {
            var appSettings = serviceProvider.GetRequiredService<AppSettings>();
            var store = serviceProvider.GetRequiredService<IStore>();
            var environment = serviceProvider.GetRequiredService<IEnvironmentService>();

            LoggerConfiguration loggerConfiguration;

            if (appSettings.Logger.LogLevel != LogLevel.None)
            {
                var loggerFilePath = Path.Combine(environment.GetBasePath(), FolderConstants.DataFolder, 
                    FolderConstants.LogsFolder, appSettings.Logger.FileName);

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

                if (environment.GetEnvironmentType() == EnvironmentType.Development)
                {
                    loggerConfiguration.WriteTo.Console(outputTemplate: appSettings.Logger.LogTemplate, theme: AnsiConsoleTheme.Code);
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