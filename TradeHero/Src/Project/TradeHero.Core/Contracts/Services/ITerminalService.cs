namespace TradeHero.Core.Contracts.Services;

public interface ITerminalService
{
    void Write(string message, ConsoleColor? consoleColor = null, ConsoleColor? backgroundColor = null);
    void WriteLine(string message, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null);
    string? ReadLine();
    void ClearConsole();
}