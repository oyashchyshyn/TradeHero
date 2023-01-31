namespace TradeHero.Core.Contracts.Services;

public interface ITerminalService
{
    void Write(string message, bool needSendWithTime, ConsoleColor? consoleColor = null);
    void WriteLine(string message, bool needSendWithTime, ConsoleColor? consoleColor = null);
    string? ReadLine();
    void ClearConsole();
}