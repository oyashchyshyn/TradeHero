using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Models.Terminal;

namespace TradeHero.Services.Services;

internal class TerminalService : ITerminalService
{
    private readonly IEnvironmentService _environmentService;

    public TerminalService(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }
    
    public void Write(string message, WriteMessageOptions? writeMessageOptions)
    {
        if (writeMessageOptions != null)
        {
            if (writeMessageOptions.FontColor.HasValue)
            {
                Console.ForegroundColor = writeMessageOptions.FontColor.Value;
            }
            
            if (writeMessageOptions.BackgroundColor.HasValue)
            {
                Console.BackgroundColor = writeMessageOptions.BackgroundColor.Value;
            }

            if (writeMessageOptions.IsMessageFinished)
            {
                message += Environment.NewLine;
            }
        }

        Console.Write(message);

        Console.ResetColor();
    }

    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    public void ClearConsole()
    {
        if (_environmentService.GetEnvironmentType() == EnvironmentType.Development)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Clear console <------------------------------------------------>");
            Console.ResetColor();
            
            return;
        }
        
        Console.Clear();
    }
}