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
            var operationSystem = args.First(x => x.StartsWith("--os=")).Replace("--os=", string.Empty);
            var environment = args.First(x => x.StartsWith("--env=")).Replace("--env=", string.Empty);

            File.Move(
                Path.Combine(baseFolderPath, mainApplicationName), 
                Path.Combine(updateFolderPath, mainApplicationName)
            );

            File.Move(
                Path.Combine(updateFolderPath, downloadedApplicationName), 
                Path.Combine(baseFolderPath, mainApplicationName)
            );

            var processStartInfo = new ProcessStartInfo();
            
            switch (operationSystem)
            {
                case "Linux":
                    processStartInfo.FileName = "/bin/bash";                                                           
                    processStartInfo.Arguments = $"{Path.Combine(baseFolderPath, mainApplicationName)} " +             
                                                 $"--upt=relaunch-app --env={environment}";
                    processStartInfo.UseShellExecute = false;
                    break;
                case "Windows":
                    processStartInfo.FileName = "cmd.exe";                                                              
                    processStartInfo.Arguments = $"/C start {Path.Combine(baseFolderPath, mainApplicationName)} " +     
                                                 $"--upt=relaunch-app --env={environment}"; 
                    processStartInfo.UseShellExecute = false;
                    break;
                default:
                    return;
            }

            Process.Start(processStartInfo);
            
            Environment.Exit(0);
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