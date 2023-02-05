using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Client;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Core.Helpers;
using TradeHero.Database;
using TradeHero.Services;
using TradeHero.Trading;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.Application;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            EnvironmentHelper.SetCulture();
            
            var environmentType = ArgsHelper.GetEnvironmentType(args);
            var appSettings = AppSettingsHelper.GenerateAppSettings(AppDomain.CurrentDomain.BaseDirectory, environmentType, RunnerType.App);
            
            ArgsHelper.IsRunAppKeyExist(args, appSettings.Application.RunAppKey);

            var cancellationTokenSource = new CancellationTokenSource();
            
            var host = HostApp.CreateDefaultBuilder(args)
                .UseEnvironment(environmentType.ToString())
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddServices(appSettings);
                    serviceCollection.AddClient();
                    serviceCollection.AddDatabase();
                    serviceCollection.AddTrading();
                    serviceCollection.AddHost(cancellationTokenSource);
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddThSerilog();
                })
                .Build();

            await host.RunAsync(token: cancellationTokenSource.Token);
        }
        catch (Exception exception)
        {
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderConstants.LogsFolder);

            await LoggerHelper.WriteLogToFileAsync(exception, logsPath, FileConstants.AppFatalLogsName);

            Environment.ExitCode = (int)AppExitCode.Failure;
        }
    }
}