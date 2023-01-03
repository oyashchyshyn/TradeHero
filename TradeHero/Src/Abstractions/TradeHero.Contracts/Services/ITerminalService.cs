namespace TradeHero.Contracts.Services;

public interface ITerminalService
{
    void Write(string message, ConsoleColor? consoleColor = null);
    void WriteLine(string message, ConsoleColor? consoleColor = null);
    string? ReadLine();
    void ClearConsole();
}