using TradeHero.Contracts.Services;

namespace TradeHero.Core.Services;

internal class TerminalService : ITerminalService
{
    public void NotifyInConsole(string message, ConsoleColor consoleColor)
    {
        Console.ForegroundColor = consoleColor;
        Console.WriteLine();
        Console.ResetColor();
    }

    public void ClearConsole()
    {
        Console.Clear();
    }
}