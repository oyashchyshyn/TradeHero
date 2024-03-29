﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Logger;

namespace TradeHero.Core.Extensions;

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
                var logInstance = environmentService.GetRunnerType() switch
                {
                    RunnerType.Launcher => appSettings.Logger.LauncherInstance,
                    RunnerType.App => appSettings.Logger.AppInstance,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var loggerFilePath = Path.Combine(environmentService.GetBasePath(),
                    appSettings.Folder.LogsFolderName, logInstance.FileName);
                
                loggerConfiguration = new LoggerConfiguration()
                    .MinimumLevel.Is((LogEventLevel)appSettings.Logger.LogLevel)
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Sink(new StoreEventSink(store))
                    .WriteTo.File
                    (
                        loggerFilePath,
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: logInstance.LogTemplate,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 209715200 // Limit file size 200mb.
                    );

                if (environmentService.GetEnvironmentType() == EnvironmentType.Development)
                {
                    loggerConfiguration.WriteTo.Console(outputTemplate: logInstance.LogTemplate, 
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