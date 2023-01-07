using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TradeHero.Updater;

internal static class Program
{
    [DllImport("libc", SetLastError = true)]
    private static extern int chmod(string pathname, int mode);
    
    private static readonly string UpdaterLogsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.txt");
    
    private static async Task Main(string[] args)
    {
        try
        {
            await WriteMessageToFileAsync($"Start update. Args: {string.Join(", ", args)}");
            
            const string environmentArg = "--env=";
            const string operationSystemArg = "--os=";
            const string downloadApplicationPathArg = "--dap=";
            const string baseApplicationPathArg = "--ap=";
            const string baseApplicationNameArg = "--ban=";

            var environment = args.First(x => x.StartsWith(environmentArg)).Replace(environmentArg, string.Empty);
            var operationSystem = args.First(x => x.StartsWith(operationSystemArg)).Replace(operationSystemArg, string.Empty);
            var downloadApplicationPath = args.First(x => x.StartsWith(downloadApplicationPathArg)).Replace(downloadApplicationPathArg, string.Empty);
            var applicationPath = args.First(x => x.StartsWith(baseApplicationPathArg)).Replace(baseApplicationPathArg, string.Empty);
            var baseApplicationName = args.First(x => x.StartsWith(baseApplicationNameArg)).Replace(baseApplicationNameArg, string.Empty);

            foreach (var tradeHeroProcess in Process.GetProcesses().Where(x => x.ProcessName.Contains(baseApplicationName)))
            {
                tradeHeroProcess.Kill(true);
                tradeHeroProcess.Dispose();
            }
            
            File.Move(downloadApplicationPath, applicationPath, true);

            var processStartInfo = new ProcessStartInfo();

            var pathWithArgs = $"{applicationPath} --update --env={environment}";
            
            switch (operationSystem)
            {
                case "Linux":
                    chmod(applicationPath, 0x1 | 0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x80 | 0x100);
                    processStartInfo.FileName = "/bin/bash";                                                           
                    processStartInfo.Arguments = $"-c \"{pathWithArgs}\"";
                    break;
                case "Windows":
                    processStartInfo.FileName = "cmd.exe";                                                              
                    processStartInfo.Arguments = $"/C start {pathWithArgs}";
                    break;
                default:
                    throw new Exception($"Cannot apply update for operation system: {operationSystem}");
            }

            processStartInfo.UseShellExecute = false;
            
            Process.Start(processStartInfo);
            
            await WriteMessageToFileAsync("Finish update.");
            
            Environment.Exit(0);
        }
        catch (Exception exception)
        {
            await WriteMessageToFileAsync(exception.ToString());
            
            Environment.Exit(-1);
        }
    }

    private static async Task WriteMessageToFileAsync(string message)
    {
        message += Environment.NewLine;
        
        await File.AppendAllTextAsync(UpdaterLogsFile, message);
    }
}