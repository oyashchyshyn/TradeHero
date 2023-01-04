using System.Diagnostics;

namespace TradeHero.Updater;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            var baseFolderPath = args.First(x => x.StartsWith("--bfp=")).Replace("--bfp=", string.Empty);
            var updateFolderPath = args.First(x => x.StartsWith("--ufp=")).Replace("--ufp=", string.Empty);
            var mainApplicationName = args.First(x => x.StartsWith("--man=")).Replace("--man=", string.Empty);
            var downloadedApplicationName = args.First(x => x.StartsWith("--dan=")).Replace("--dan=", string.Empty);

            File.Move(
                Path.Combine(baseFolderPath, mainApplicationName), 
                Path.Combine(updateFolderPath, mainApplicationName)
            );

            File.Move(
                Path.Combine(updateFolderPath, downloadedApplicationName), 
                Path.Combine(baseFolderPath, mainApplicationName)
            );

            var process = new Process();
            process.StartInfo.WorkingDirectory = baseFolderPath;
            process.StartInfo.FileName = mainApplicationName;
            process.StartInfo.Arguments = "--upt=after-update";
            process.Start();
        }
        catch (Exception exception)
        {
            var parentDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory);
            if (parentDirectory == null)
            {
                return;
            }

            var directoryPath = Path.Combine(parentDirectory.FullName, "logs");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await File.WriteAllTextAsync(
                Path.Combine(directoryPath, "updater-fatal.txt"), exception.ToString()
            );
        }
    }
}