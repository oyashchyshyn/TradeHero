using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Application.Host;
using TradeHero.Contracts.Extensions;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;
using TradeHero.Dependencies;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.Application;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        EnvironmentHelper.SetCulture();
        
        var configuration = ConfigurationHelper.GenerateConfiguration(args);
        var environmentSettings = ConfigurationHelper.ConvertConfigurationToAppSettings(configuration);
        var environmentType = ArgsHelper.GetEnvironmentType(args);

        try
        {
            ArgsHelper.IsRunAppKeyExist(args, environmentSettings.Application.RunAppKey);
            
            var host = HostApp.CreateDefaultBuilder(args)
                .UseEnvironment(environmentType.ToString())
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .UseRunningType(RunnerType.App.ToString())
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddConfiguration(configuration);
                })
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddServices();
                    serviceCollection.AddClient();
                    serviceCollection.AddDatabase();
                    serviceCollection.AddTradingLogic();
                    serviceCollection.AddHost();
                    
                    serviceCollection.AddSingleton<IHostLifetime, AppHostLifeTime>();
                    serviceCollection.AddHostedService<AppHostedService>();
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddThSerilog();
                })
                .Build();

            await host.RunAsync();

            var store = host.Services.GetRequiredService<IStoreService>();
            
            if (store.Application.Update.IsNeedToUpdateApplication)
            {
                return (int)AppExitCode.Update;
            }
            
            return (int)AppExitCode.Success;
        }
        catch (Exception exception)
        {
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                environmentSettings.Folder.DataFolderName, environmentSettings.Folder.LogsFolderName);

            await LoggerHelper.WriteLogToFileAsync(exception, logsPath, "app_fatal.txt");
            
            return (int)AppExitCode.Failure;
        }
    }
}