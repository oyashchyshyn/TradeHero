using System.Diagnostics;
using System.Runtime.InteropServices;

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
            
            foreach (var tradeHeroProcess in Process.GetProcesses().Where(x => x.ProcessName.Contains("trade_hero")))
            {
                tradeHeroProcess.Kill();
                tradeHeroProcess.Dispose();
            }
            
            File.Move(
                Path.Combine(updateFolderPath, downloadedApplicationName), 
                Path.Combine(baseFolderPath, mainApplicationName),
                true
            );
            
            var processStartInfo = new ProcessStartInfo();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                processStartInfo.FileName = Path.Combine(baseFolderPath, mainApplicationName);
                processStartInfo.Arguments = "--upt=after-update";
                processStartInfo.UseShellExecute = false;
                processStartInfo.Verb = "runas";
            }
            
            var process = new Process
            {
                StartInfo = processStartInfo
            };
            
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