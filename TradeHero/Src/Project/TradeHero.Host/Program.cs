using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Client;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Services;
using TradeHero.Core.Helpers;
using TradeHero.Database;
using TradeHero.Host.Data;
using TradeHero.Host.Data.Dtos.Instance;
using TradeHero.Host.Data.Dtos.TradeLogic;
using TradeHero.Host.Data.Validations;
using TradeHero.Host.Dictionary;
using TradeHero.Host.Host;
using TradeHero.Host.Menu;
using TradeHero.Host.Menu.Console;
using TradeHero.Host.Menu.Telegram;
using TradeHero.Services;
using TradeHero.StrategyRunner;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.Host;

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
                    serviceCollection.AddThServices();
                    serviceCollection.AddThClient();
                    serviceCollection.AddThDatabase();
                    serviceCollection.AddThStrategyRunner();
                    
                    // Host
                    serviceCollection.AddHostedService<ThHostedService>();
                    serviceCollection.AddSingleton<IHostLifetime, ThHostLifeTime>();

                    // Menu factory
                    serviceCollection.AddSingleton<MenuFactory>();
        
                    // Menus
                    ConsoleDiContainer.Register(serviceCollection);
                    TelegramDiContainer.Register(serviceCollection);

                    // Dictionary
                    serviceCollection.AddSingleton<EnumDictionary>();

                    // Data validation
                    serviceCollection.AddTransient<IValidator<ConnectionDto>, ConnectionDtoValidation>();
                    serviceCollection.AddTransient<IValidator<PercentLimitTradeLogicDto>, PercentLimitStrategyDtoValidation>();
                    serviceCollection.AddTransient<IValidator<PercentMoveTradeLogicDto>, PercentMoveStrategyDtoValidation>();
                    serviceCollection.AddTransient<IValidator<SpotClusterVolumeOptionsDto>, SpotClusterVolumeOptionsDtoValidation>();
                    serviceCollection.AddSingleton<DtoValidator>();
                })
                .Build();
            
            if (!await host.Services.GetRequiredService<IStartupService>().CheckIsFirstRunAsync())
            {
                MessageHelper.WriteError("There is an error during user creation. Please see logs.");

                return;
            }

            await host.RunAsync();

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