namespace TradeHero.Contracts.Sockets;

public interface ISocketClient
{
    Task ConnectAsync();
    Task SendMessageAsync(string message);
    void Close();
}