using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Sockets;

public interface IClientSocket
{
    event EventHandler<ListenerCommand> OnReceiveMessageFromServer;
    void Connect();
    void Close();
    void SendMessage(string message);
}