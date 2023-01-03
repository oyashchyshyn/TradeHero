using TradeHero.Contracts.Base.Constants;

namespace TradeHero.Runner.Helpers;

internal static class ExceptionHelper
{
    public static async Task WriteExceptionAsync(Exception exception, string baseDirectory)
    {            
        var directoryPath = Path.Combine(baseDirectory, FolderConstants.LogsFolder);
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
}