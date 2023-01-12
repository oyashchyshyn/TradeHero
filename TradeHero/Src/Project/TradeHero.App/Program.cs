using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TradeHero.App.Host;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;
using TradeHero.Dependencies;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.App;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        EnvironmentHelper.SetCulture();

        if (!args.Contains(ArgumentKeyConstants.RunApp))
        {
            MessageHelper.WriteError("Cannot start app!");
                
            return (int)AppExitCode.Failure;
        }
        
        var configuration = ConfigurationHelper.GenerateConfiguration(args);
        var environmentSettings = ConfigurationHelper.ConvertConfigurationToAppSettings(configuration);
        var environmentType = ArgsHelper.GetEnvironmentType(args);

        try
        {
            var host = HostApp.CreateDefaultBuilder(args)
                .UseEnvironment(environmentType.ToString())
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
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
                    
                    serviceCollection.AddHostedService<AppHostedService>();
                    serviceCollection.AddSingleton<IHostLifetime, AppHostLifeTime>();
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog();
                })
                .Build();

            await host.RunAsync();

            var environmentServices = host.Services.GetRequiredService<IEnvironmentService>();
            
            if (environmentServices.CustomArgs.ContainsKey(ArgumentKeyConstants.Update))
            {
                return (int)AppExitCode.Update;
            }
            
            return (int)AppExitCode.Success;
        }
        catch (Exception exception)
        {
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                environmentSettings.Folder.DataFolderName, environmentSettings.Folder.LogsFolderName);
            
            await MessageHelper.WriteErrorAsync(exception, logsPath);
            
            return (int)AppExitCode.Failure;
        }
    }
}