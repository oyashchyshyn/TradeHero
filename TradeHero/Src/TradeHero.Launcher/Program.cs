﻿using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Extensions;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;
using TradeHero.Dependencies;
using TradeHero.Launcher.Host;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.Launcher;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var configuration = ConfigurationHelper.GenerateConfiguration(args);
        var environmentSettings = ConfigurationHelper.ConvertConfigurationToAppSettings(configuration);
        var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, environmentSettings.Folder.DataFolderName);

        try
        {
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }
            
            if (Process.GetProcesses().Count(x => x.ProcessName == Process.GetCurrentProcess().ProcessName) > 1)
            {
                throw new Exception("Bot already running!");
            }

            EnvironmentHelper.SetCulture();
            
            var environmentType = ArgsHelper.GetEnvironmentType(args);

            var host = HostApp.CreateDefaultBuilder(args)
                .UseEnvironment(environmentType.ToString())
                .UseContentRoot(baseDirectory)
                .UseRunningType(RunnerType.Launcher.ToString())
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddConfiguration(configuration);
                })
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddServices();
                    serviceCollection.AddDatabase();
                    
                    serviceCollection.AddSingleton<IHostLifetime, LauncherHostedLifeTime>();
                    serviceCollection.AddHostedService<LauncherHostedService>();
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddThSerilog();
                })
                .Build();

            await host.RunAsync();

            return (int)AppExitCode.Success;
        }
        catch (Exception exception)
        {
            await MessageHelper.WriteErrorAsync(exception, 
                Path.Combine(baseDirectory, environmentSettings.Folder.LogsFolderName));
            
            return (int)AppExitCode.Failure;
        }
    }
}