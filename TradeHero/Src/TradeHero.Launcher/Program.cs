using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;
using TradeHero.Launcher.Providers;
using TradeHero.Launcher.Services;

namespace TradeHero.Launcher;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var environmentType = ArgsHelper.GetEnvironmentType(args);
        var appSettings = ConfigurationHelper.ConvertConfigurationToAppSettings(
            ConfigurationHelper.GenerateConfiguration(AppDomain.CurrentDomain.BaseDirectory, environmentType, RunnerType.Launcher)
        );
        
        var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appSettings.Folder.DataFolderName);
        
        try
        {
            if (Process.GetProcesses().Count(x => x.ProcessName == Process.GetCurrentProcess().ProcessName) > 1)
            {
                throw new Exception("Bot already running!");
            }
        
            EnvironmentHelper.SetCulture();
        
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }
        
            var configurationForLauncher = ConfigurationHelper.GenerateConfiguration(baseDirectory, environmentType, RunnerType.Launcher);
            await using (var serviceProvider = LauncherServiceProvider.Build(configurationForLauncher, new CancellationTokenSource()))
            {
                var launcherService = serviceProvider.GetRequiredService<LauncherStartupService>();
                
                launcherService.Start();
                
                if (!await launcherService.ManageDatabaseDataAsync())
                {
                    throw new Exception("Cannot manage with database.");
                }
        
                launcherService.RunApp();

                launcherService.AppWaiting.WaitOne();
                
                launcherService.Finish();
            }
        
            Environment.ExitCode = (int)AppExitCode.Success;
        }
        catch (Exception exception)
        {
            var logsPath = Path.Combine(baseDirectory, appSettings.Folder.LogsFolderName);
            await LoggerHelper.WriteLogToFileAsync(exception, logsPath, "launcher_fatal.txt");
        
            await MessageHelper.WriteMessageAsync(exception.Message);
        
            Environment.ExitCode = (int)AppExitCode.Failure;
        }
    }
}