using System.Diagnostics;

namespace TradeHero.Updater;

internal static class Program
{
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
        }
    }

    private static async Task WriteMessageToFileAsync(string message)
    {
        message += Environment.NewLine;
        
        await File.AppendAllTextAsync(UpdaterLogsFile, message);
    }
}