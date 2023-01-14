using Microsoft.Extensions.DependencyInjection;
using TradeHero.Client;
using TradeHero.Database;
using TradeHero.Main;
using TradeHero.Services;
using TradeHero.Sockets;
using TradeHero.Trading;

namespace TradeHero.Dependencies;

public static class ThServiceCollectionExtensions
{
    public static void AddClient(this IServiceCollection serviceCollection)
    {
        ClientDiContainer.Register(serviceCollection);
    }
    
    public static void AddDatabase(this IServiceCollection serviceCollection)
    {
        DatabaseDiContainer.Register(serviceCollection);
    }
    
    public static void AddHost(this IServiceCollection serviceCollection)
    {
        HostDiContainer.Register(serviceCollection);
    }
    
    public static void AddServices(this IServiceCollection serviceCollection)
    {
        ServicesDiContainer.Register(serviceCollection);
    }
    
    public static void AddTrading(this IServiceCollection serviceCollection)
    {
        TradingDiContainer.Register(serviceCollection);
    }
    
    public static void AddSockets(this IServiceCollection serviceCollection)
    {
        SocketsDiContainer.Register(serviceCollection);
    }
}