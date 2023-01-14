using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Sockets;

public interface IServerSocket
{
    event EventHandler<ListenerCommand> OnReceiveMessageFromClient;
    void StartListen();
    void DisconnectClient();
    void Close();
    Task SendMessageAsync(string message);
}