using TradeHero.Contracts.Sockets.Args;

namespace TradeHero.Contracts.Sockets;

public interface ISocketClient
{
    event EventHandler<SocketMessageArgs> OnReceiveMessageFromServer;
    Task ConnectAsync();
    Task SendMessageAsync(string message);
    void Close();
}