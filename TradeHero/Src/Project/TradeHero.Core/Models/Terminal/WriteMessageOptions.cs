namespace TradeHero.Core.Models.Terminal;

public class WriteMessageOptions
{
    public bool IsMessageFinished { get; set; }
    public ConsoleColor? FontColor { get; set; }
    public ConsoleColor? BackgroundColor { get; set; }
}