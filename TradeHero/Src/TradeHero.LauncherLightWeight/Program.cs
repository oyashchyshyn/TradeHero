using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;
using TradeHero.LauncherLightWeight.Helpers;
using TradeHero.LauncherLightWeight.Services;

namespace TradeHero.LauncherLightWeight;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        ILoggerFactory? loggerFactory = null;
        
        var configuration = ConfigurationHelper.GenerateConfiguration(args);
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

            loggerFactory = LoggerGenerator.GenerateLoggerFactory(baseDirectory, appSettings,
                environmentType, RunnerType.Launcher);

            var launcherEnvironment = new LauncherEnvironment(appSettings, environmentType, baseDirectory);
            
            var launcherContainer = new LauncherContainer(
                loggerFactory.CreateLogger<LauncherContainer>(), 
                launcherEnvironment
            );

            launcherContainer.Start();
            
            launcherContainer.Dispose();
            
            Environment.ExitCode = (int)AppExitCode.Success;
        }
        catch (Exception exception)
        {
            var logsPath = Path.Combine(baseDirectory, appSettings.Folder.LogsFolderName);
            await LoggerHelper.WriteLogToFileAsync(exception, logsPath, "launcher_fatal.txt");

            await MessageHelper.WriteMessageAsync(exception.Message);

            Environment.ExitCode = (int)AppExitCode.Failure;
        }
        finally
        {
            loggerFactory?.Dispose();
        }
    }
}