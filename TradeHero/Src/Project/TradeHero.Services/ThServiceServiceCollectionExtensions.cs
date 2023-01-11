using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Services.Services;

namespace TradeHero.Services;

public static class ThServiceServiceCollectionExtensions
{
    public static void AddThServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IJsonService, JsonService>();
        serviceCollection.AddSingleton<ICalculatorService, CalculatorService>();
        serviceCollection.AddSingleton<IDateTimeService, DateTimeService>();
        serviceCollection.AddSingleton<IJobService, JobService>();
        serviceCollection.AddSingleton<ITelegramService, TelegramService>();
        serviceCollection.AddSingleton<IInternetConnectionService, InternetConnectionService>();
        serviceCollection.AddSingleton<IEnvironmentService, EnvironmentService>();
        serviceCollection.AddSingleton<IFileService, FileService>();
        serviceCollection.AddSingleton<IGithubService, GithubService>();
        serviceCollection.AddSingleton<ITerminalService, TerminalService>();
        serviceCollection.AddSingleton<IStartupService, StartupService>();
        serviceCollection.AddSingleton<IStoreService, StoreServiceService>();
        
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