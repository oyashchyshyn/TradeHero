using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Client;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;
using TradeHero.Database;
using TradeHero.Services;
using TradeHero.Trading;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.Host;

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
                    serviceCollection.AddThServices();
                    serviceCollection.AddThClient();
                    serviceCollection.AddThDatabase();
                    serviceCollection.AddThStrategyRunner();
                    
                    HostDiContainer.Register(serviceCollection);
                })
                .Build();

            if (!await host.Services.GetRequiredService<IStartupService>().CheckIsFirstRunAsync())
            {
                MessageHelper.WriteError("There is an error during user creation. Please see logs.");

                return (int)AppExitCode.Failure;
            }

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