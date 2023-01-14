namespace TradeHero.Contracts.Sockets.Args;

public class SocketMessageArgs
{
    public string Message { get; } = string.Empty;

    public SocketMessageArgs(string message)
    {
        Message = message;
    }
}