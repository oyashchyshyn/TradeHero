using TradeHero.Contracts.Sockets.Args;

namespace TradeHero.Contracts.Sockets;

public interface IServerSocket
{
    event EventHandler<SocketMessageArgs> OnReceiveMessageFromClient;
    void StartListen();
    void DisconnectClient();
    void Close();
}