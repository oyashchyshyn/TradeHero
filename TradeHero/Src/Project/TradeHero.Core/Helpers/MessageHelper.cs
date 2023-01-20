namespace TradeHero.Core.Helpers;

public static class MessageHelper
{
    public static Task WriteMessageAsync(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
        Console.WriteLine("Press any key for exit...");
        Console.ReadLine();
        
        return Task.CompletedTask;
    }
}