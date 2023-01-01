using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;

namespace TradeHero.Core.Services;

internal class TerminalService : ITerminalService
{
    private readonly IEnvironmentService _environmentService;

    public TerminalService(
        IEnvironmentService environmentService
        )
    {
        _environmentService = environmentService;
    }
    
    public void NotifyInConsole(string message, ConsoleColor consoleColor)
    {
        Console.ForegroundColor = consoleColor;
        Console.WriteLine();
        Console.ResetColor();
    }

    public void ClearConsole()
    {
        if (_environmentService.GetEnvironmentType() == EnvironmentType.Development)
        {
            Console.WriteLine("Clear console <------------------------------------------------>");
            
            return;
        }
        
        Console.Clear();
    }
}