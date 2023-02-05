using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;
using TradeHero.Launcher.Providers;
using TradeHero.Launcher.Services;

namespace TradeHero.Launcher;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            TerminalHelper.SetTerminalTitle("trade_hero");
            EnvironmentHelper.SetCulture();
            
            if (Process.GetProcesses().Count(x => x.ProcessName == Process.GetCurrentProcess().ProcessName) > 1)
            {
                throw new Exception("Bot already running!");
            }
            
            var environmentType = ArgsHelper.GetEnvironmentType(args);
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderConstants.DataFolder);

            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }
            
            var appSettings = AppSettingsHelper.GenerateAppSettings(baseDirectory, environmentType, RunnerType.Launcher);
            
            await using (var serviceProvider = LauncherServiceProvider.Build(appSettings))
            {
                var launcherService = serviceProvider.GetRequiredService<LauncherStartupService>();
                
                launcherService.Start();
                
                if (!await launcherService.ManageDatabaseDataAsync())
                {
                    throw new Exception("Cannot manage with local data, please see logs.");
                }
        
                launcherService.RunApp();

                launcherService.AppWaiting.WaitOne();
                
                launcherService.Finish();
            }
        
            Environment.ExitCode = (int)AppExitCode.Success;
        }
        catch (Exception exception)
        {
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderConstants.DataFolder, FolderConstants.LogsFolder);
            await LoggerHelper.WriteLogToFileAsync(exception, logsPath, FileConstants.LauncherFatalLogsName);
        
            TerminalHelper.WriteMessage(exception.Message);
        
            Environment.ExitCode = (int)AppExitCode.Failure;
        }
    }
}