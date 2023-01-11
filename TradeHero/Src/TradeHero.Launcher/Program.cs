using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;
using TradeHero.Launcher.Host;
using TradeHero.Launcher.Logger;
using TradeHero.Launcher.Services;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.Launcher;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var configuration = ConfigurationHelper.GenerateConfiguration(args);
        var environmentSettings = ConfigurationHelper.ConvertConfigurationToAppSettings(configuration);

        try
        {
            if (Process.GetProcesses().Count(x => x.ProcessName == Process.GetCurrentProcess().ProcessName) > 1)
            {
                MessageHelper.WriteError("Bot already running!");
                
                return (int)AppExitCode.Failure;
            }
            
            EnvironmentHelper.SetCulture();
            
            var environmentType = ArgsHelper.GetEnvironmentType(args);

            var host = HostApp.CreateDefaultBuilder(args)
                .UseEnvironment(environmentType.ToString())
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddConfiguration(configuration);
                })
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddSingleton<EnvironmentService>();
                    serviceCollection.AddSingleton<GithubService>();
                    serviceCollection.AddSingleton<IHostLifetime, LauncherHostedLifeTime>();
                    serviceCollection.AddHostedService<LauncherHostedService>();
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog();
                })
                .Build();

            await host.RunAsync();

            return (int)AppExitCode.Success;
        }
        catch (Exception exception)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                environmentSettings.Folder.DataFolderName, environmentSettings.Folder.LogsFolderName);
            
            await MessageHelper.WriteErrorAsync(exception, path);
            
            return (int)AppExitCode.Failure;
        }
    }
}