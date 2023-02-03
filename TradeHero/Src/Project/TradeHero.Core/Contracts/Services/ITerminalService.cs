using TradeHero.Core.Models.Terminal;

namespace TradeHero.Core.Contracts.Services;

public interface ITerminalService
{
    void Write(string message, WriteMessageOptions? writeMessageOptions = null);
    void SetTerminalTitle(string title);
    string? ReadLine();
    void ClearConsole();
}