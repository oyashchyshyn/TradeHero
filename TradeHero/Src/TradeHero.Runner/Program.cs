using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Services;
using TradeHero.Core.Helpers;
using TradeHero.DependencyResolver;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.Runner;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = ConfigurationHelper.GenerateConfiguration(args);
        var environmentSettings = ConfigurationHelper.ConvertConfigurationToAppSettings(configuration);

        try
        {
            EnvironmentHelper.SetCulture();

            if (Process.GetProcesses().Count(x => x.ProcessName == environmentSettings.Application.BaseAppName) > 1)
            {
                MessageHelper.WriteError("Bot already running!");
                
                return;
            }
            
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
                    serviceCollection.AddThDependencyCollection();
                })
                .Build();
            
            if (!await host.Services.GetRequiredService<IStartupService>().CheckIsFirstRunAsync())
            {
                MessageHelper.WriteError("There is an error during user creation. Please see logs.");

                return;
            }
            
            var updateService = host.Services.GetRequiredService<IUpdateService>();
            
            await host.RunAsync();

            if (updateService.IsNeedToUpdate)
            {
                await updateService.StartUpdateAsync();
            }
            
            Environment.Exit(0);
        }
        catch (Exception exception)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                environmentSettings.Folder.DataFolderName, environmentSettings.Folder.LogsFolderName);
            
            await MessageHelper.WriteErrorAsync(exception, path);
            
            Environment.Exit(-1);
        }
    }
}