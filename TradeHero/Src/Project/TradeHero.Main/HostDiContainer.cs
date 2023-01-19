using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TradeHero.Core.Types.Menu;
using TradeHero.Core.Types.Repositories;
using TradeHero.Core.Types.Repositories.Models;
using TradeHero.Core.Types.Services;
using TradeHero.Main.Data;
using TradeHero.Main.Data.Dtos.Instance;
using TradeHero.Main.Data.Dtos.TradeLogic;
using TradeHero.Main.Data.Validations;
using TradeHero.Main.Dictionary;
using TradeHero.Main.Menu;
using TradeHero.Main.Menu.Console;
using TradeHero.Main.Menu.Telegram;
using TradeHero.Main.Telegram;

namespace TradeHero.Main;

public static class HostDiContainer
{
    public static void AddHost(this IServiceCollection serviceCollection)
    {
        // Menu factory
        serviceCollection.AddSingleton<IMenuFactory, MenuFactory>();
        
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