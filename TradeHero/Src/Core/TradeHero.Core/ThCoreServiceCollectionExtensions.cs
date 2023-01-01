using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Settings;
using TradeHero.Contracts.Store;
using TradeHero.Core.Logger;
using TradeHero.Core.Services;
using TradeHero.Core.Store;

namespace TradeHero.Core;

public static class ThCoreServiceCollectionExtensions
{
    public static void AddThCore(this IServiceCollection serviceCollection)
    {
        // AppSettings
        serviceCollection.AddSingleton(GetAppSettings());
        
        // Store
        serviceCollection.AddSingleton<IStore, ApplicationStore>();

        // Services
        serviceCollection.AddSingleton<IJsonService, JsonService>();
        serviceCollection.AddSingleton<ICalculatorService, CalculatorService>();
        serviceCollection.AddSingleton<IDateTimeService, DateTimeService>();
        serviceCollection.AddSingleton<IJobService, JobService>();
        serviceCollection.AddSingleton<ITelegramService, TelegramService>();
        serviceCollection.AddSingleton<IInternetConnectionService, InternetConnectionService>();
        serviceCollection.AddSingleton<IEnvironmentService, EnvironmentService>();
        serviceCollection.AddSingleton<IFileService, FileService>();
        serviceCollection.AddSingleton<IUpdateService, UpdateService>();
        serviceCollection.AddSingleton<ITerminalService, TerminalService>();
        
        serviceCollection.AddHttpClient("TelegramBotClient")
            .AddTypedClient<ITelegramBotClient>((httpClient, serviceProvider) =>
            {
                var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
                var options = new TelegramBotClientOptions(userRepository.GetUser().TelegramBotToken);
                return new TelegramBotClient(options, httpClient);
            });
        
        // Logger
        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddThSerilog();
        });
    }

    #region Private methods

    private static AppSettings GetAppSettings()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly == null)
        {
            throw new Exception("Cannot load main assembly");
        }
        
        using var stream = assembly.GetManifestResourceStream("TradeHero.Runner.app.json");
        if (stream == null)
        {
            throw new Exception("Cannot find app.json");
        }

        var appSettingsConfiguration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        var appSettings = appSettingsConfiguration.Get<AppSettings>();
        if (appSettings == null)
        {
            throw new Exception("There is no app.json");
        }

        return appSettings;
    }

    #endregion
}