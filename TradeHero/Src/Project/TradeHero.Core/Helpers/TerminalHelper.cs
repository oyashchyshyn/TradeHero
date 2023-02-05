namespace TradeHero.Core.Helpers;

public static class TerminalHelper
{
    public static void SetTerminalTitle(string title)
    {
        Console.Title = title;
    }
    
    public static void WriteMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
        Console.WriteLine("Press any key for exit...");
        Console.ReadLine();
    }
}