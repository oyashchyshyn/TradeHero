using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Store;
using TradeHero.Core.Logger;
using TradeHero.Core.Services;
using TradeHero.Core.Store;

namespace TradeHero.Core;

public static class ThCoreServiceCollectionExtensions
{
    public static void AddThCore(this IServiceCollection serviceCollection)
    {
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
}