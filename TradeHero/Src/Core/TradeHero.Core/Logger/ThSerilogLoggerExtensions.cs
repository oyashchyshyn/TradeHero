using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;
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
            var store = serviceProvider.GetRequiredService<IStore>();
            var environmentService = serviceProvider.GetRequiredService<IEnvironmentService>();
            var environmentSettings = environmentService.GetEnvironmentSettings();

            LoggerConfiguration loggerConfiguration;

            if (environmentSettings.Logger.LogLevel != LogLevel.None)
            {
                var loggerFilePath = Path.Combine(environmentService.GetLogsFolderPath(), 
                    environmentSettings.Logger.FileName);

                loggerConfiguration = new LoggerConfiguration()
                    .MinimumLevel.Is((LogEventLevel)environmentSettings.Logger.LogLevel)
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning) 
                    .Enrich.FromLogContext()
                    .WriteTo.Sink(new StoreEventSink(store))
                    .WriteTo.File
                    (
                        loggerFilePath,
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: environmentSettings.Logger.LogTemplate,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 209715200 // Limit file size 200mb.
                    );

                if (environmentService.GetEnvironmentType() == EnvironmentType.Development)
                {
                    loggerConfiguration.WriteTo.Console(outputTemplate: environmentSettings.Logger.LogTemplate, 
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