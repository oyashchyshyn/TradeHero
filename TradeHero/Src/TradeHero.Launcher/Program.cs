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
    private static async Task Main(string[] args)
    {
        var configuration = ConfigurationHelper.GenerateConfiguration(args);
        configuration[HostConstants.RunnerType] = RunnerType.Launcher.ToString();
        
        var appSettings = ConfigurationHelper.ConvertConfigurationToAppSettings(configuration);
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

            var environmentType = ArgsHelper.GetEnvironmentType(args);

            await using (var serviceProvider = LauncherServiceProvider.Build(configuration, baseDirectory, environmentType))
            {
                var launcherService = serviceProvider.GetRequiredService<LauncherStartupService>();
                
                launcherService.Start();
                
                if (!await launcherService.ManageDatabaseDataAsync())
                {
                    throw new Exception("Cannot manage with database.");
                }

                launcherService.RunApp();
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