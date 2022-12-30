namespace TradeHero.Contracts.Services;

public interface ITerminalService
{
    void NotifyInConsole(string message, ConsoleColor consoleColor);
    void ClearConsole();
}