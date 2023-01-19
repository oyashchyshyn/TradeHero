using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Application.Host;
using TradeHero.Client;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Core.Helpers;
using TradeHero.Database;
using TradeHero.Main;
using TradeHero.Services;
using TradeHero.Trading;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.Application;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        EnvironmentHelper.SetCulture();
        
        var configuration = ConfigurationHelper.GenerateConfiguration(args);
        var environmentSettings = ConfigurationHelper.ConvertConfigurationToAppSettings(configuration);
        var environmentType = ArgsHelper.GetEnvironmentType(args);
        var cancellationTokenSource = new CancellationTokenSource();

        try
        {
            configuration = ConfigurationHelper.SetEnvironmentProperties(configuration, 
                AppDomain.CurrentDomain.BaseDirectory, RunnerType.App, environmentType);
            
            ArgsHelper.IsRunAppKeyExist(args, environmentSettings.Application.RunAppKey);

            var host = HostApp.CreateDefaultBuilder(args)
                .UseEnvironment(environmentType.ToString())
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddConfiguration(configuration);
                })
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddServices(cancellationTokenSource);
                    serviceCollection.AddClient();
                    serviceCollection.AddDatabase();
                    serviceCollection.AddTrading();
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

            await host.RunAsync(token: cancellationTokenSource.Token);
        }
        catch (Exception exception)
        {
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, environmentSettings.Folder.LogsFolderName);

            await LoggerHelper.WriteLogToFileAsync(exception, logsPath, "app_fatal.txt");

            Environment.ExitCode = (int)AppExitCode.Failure;
        }
    }
}