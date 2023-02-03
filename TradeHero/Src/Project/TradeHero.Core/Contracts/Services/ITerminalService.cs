using TradeHero.Core.Models.Terminal;

namespace TradeHero.Core.Contracts.Services;

public interface ITerminalService
{
    void Write(string message, WriteMessageOptions? writeMessageOptions = null);
    string? ReadLine();
    void ClearConsole();
}