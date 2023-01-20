namespace TradeHero.Core.Helpers;

public static class LoggerHelper
{
    public static async Task WriteLogToFileAsync(Exception exception, string folderPath, string fileName)
    {
        var directoryPath = Path.Combine(folderPath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var message = $"{DateTimeOffset.UtcNow} {string.Join(string.Empty, exception.ToString(), Environment.NewLine)}";
        
        await File.AppendAllTextAsync(Path.Combine(directoryPath, fileName), message);
    }
}