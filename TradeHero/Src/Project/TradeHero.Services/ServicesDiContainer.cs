using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TradeHero.Core.Types.Repositories;
using TradeHero.Core.Types.Services;
using TradeHero.Main.Telegram;
using TradeHero.Services.Services;

namespace TradeHero.Services;

public static class ServicesDiContainer
{
    public static void AddServices(this IServiceCollection serviceCollection, CancellationTokenSource cancellationTokenSource)
    {
        serviceCollection.AddSingleton<IJsonService, JsonService>();
        serviceCollection.AddSingleton<ICalculatorService, CalculatorService>();
        serviceCollection.AddSingleton<IDateTimeService, DateTimeService>();
        serviceCollection.AddSingleton<IJobService, JobService>();
        serviceCollection.AddSingleton<IInternetConnectionService, InternetConnectionService>();
        serviceCollection.AddSingleton<IFileService, FileService>();
        serviceCollection.AddSingleton<IGithubService, GithubService>();
        serviceCollection.AddSingleton<ITerminalService, TerminalService>();
        serviceCollection.AddSingleton<IStoreService, StoreService>();
        
        // Envrionment
        serviceCollection.AddSingleton<IEnvironmentService>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new EnvironmentService(loggerFactory.CreateLogger<EnvironmentService>(), configuration,
                cancellationTokenSource);
        });
        
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