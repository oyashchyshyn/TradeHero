using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Sockets;

namespace TradeHero.Sockets;

public static class SocketsDiContainer
{
    public static void Register(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<ISocketClient, SocketClient>();
        serviceCollection.AddTransient<IServerSocket, ServerSocket>();
    }
}