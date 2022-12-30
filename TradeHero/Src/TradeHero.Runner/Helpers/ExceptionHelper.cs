using TradeHero.Contracts.Base.Constants;

namespace TradeHero.Runner.Helpers;

public static class ExceptionHelper
{
    public static async Task WriteExceptionAsync(Exception exception)
    {            
        var directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderConstants.LogsFolder);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
            
        await File.WriteAllTextAsync(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderConstants.LogsFolder, "fatal.txt"), 
            exception.ToString()
        );
            
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"FATAL: {exception.Message}");
        Console.ResetColor();
        Console.WriteLine("Press any key for exit...");
        Console.ReadLine();
    }
}