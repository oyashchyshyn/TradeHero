using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TradeHero.Core.Contracts.Repositories;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Contracts.Settings;
using TradeHero.Main.Telegram;
using TradeHero.Services.Services;

namespace TradeHero.Services;

public static class ServicesDiContainer
{
    public static void AddServices(this IServiceCollection serviceCollection, AppSettings appSettings)
    {
        // Services
        serviceCollection.AddSingleton<IJsonService, JsonService>();
        serviceCollection.AddSingleton<ICalculatorService, CalculatorService>();
        serviceCollection.AddSingleton<IDateTimeService, DateTimeService>();
        serviceCollection.AddSingleton<IJobService, JobService>();
        serviceCollection.AddSingleton<IInternetConnectionService, InternetConnectionService>();
        serviceCollection.AddSingleton<IFileService, FileService>();
        serviceCollection.AddSingleton<IGithubService, GithubService>();
        serviceCollection.AddSingleton<ITerminalService, TerminalService>();

        // Store
        serviceCollection.AddSingleton<IStoreService, StoreService>();
        
        // Environment
        serviceCollection.AddSingleton<IEnvironmentService>(_ => new EnvironmentService(appSettings));

        // Telegram
        serviceCollection.AddSingleton<ITelegramService, TelegramService>();
        serviceCollection.AddHttpClient("TelegramBotClient")
            .AddTypedClient<ITelegramBotClient>((httpClient, serviceProvider) =>
            {
                var botToken = "default";
                var activeUser = serviceProvider.GetRequiredService<IUserRepository>().GetActiveUser();
                if (activeUser != null)
                {
                    botToken = activeUser.TelegramBotToken;
                }
                
                var options = new TelegramBotClientOptions(botToken);
                return new TelegramBotClient(options, httpClient);
            });
    }
}