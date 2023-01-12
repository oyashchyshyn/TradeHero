namespace TradeHero.Core.Helpers;

public static class MessageHelper
{
    public static async Task WriteErrorAsync(Exception exception, string logsFolderPath)
    {            
        var directoryPath = Path.Combine(logsFolderPath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
            
        await File.WriteAllTextAsync(
            Path.Combine(directoryPath, "fatal.txt"), exception.ToString()
        );
            
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"FATAL: {exception.Message}");
        Console.ResetColor();
        Console.WriteLine("Press any key for exit...");
        Console.ReadLine();
    }
    
    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ResetColor();
        Console.WriteLine("Press any key for exit...");
        Console.ReadLine();
    }
}