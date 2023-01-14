using TradeHero.Contracts.Sockets.Args;

namespace TradeHero.Contracts.Sockets;

public interface ISocketClient
{
    event EventHandler<SocketMessageArgs> OnReceiveMessageFromServer;
    void Connect();
    void Close();
    void SendMessage(string message);
}