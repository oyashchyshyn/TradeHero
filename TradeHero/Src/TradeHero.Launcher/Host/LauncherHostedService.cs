using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Launcher.Services;

namespace TradeHero.Launcher.Host;

internal class LauncherHostedService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IApplicationService _applicationService;
    private readonly IEnvironmentService _environmentService;

    private readonly AppService _appService;
    
    public LauncherHostedService(
        ILoggerFactory loggerFactory, 
        IApplicationService applicationService,
        IEnvironmentService environmentService, 
        AppService appService
        )
    {
        _logger = loggerFactory.CreateLogger("TradeHero.Launcher");
        _environmentService = environmentService;
        _applicationService = applicationService;
        _appService = appService;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Launcher started. Press Ctrl+C to shut down");
        _logger.LogInformation("Process id: {ProcessId}", _environmentService.GetCurrentProcessId());
        _logger.LogInformation("Base path: {GetBasePath}", _environmentService.GetBasePath());
        _logger.LogInformation("Environment: {GetEnvironmentType}", _environmentService.GetEnvironmentType());
        _logger.LogInformation("Runner type: {RunnerType}", _environmentService.GetRunnerType());

        _applicationService.SetActionsBeforeStopApplication(StopApp);
        
        if (_environmentService.GetEnvironmentType() == EnvironmentType.Development)
        {
            _logger.LogInformation("Args: {GetBasePath}", string.Join(", ", _environmentService.GetEnvironmentArgs()));   
        }

        _appService.StartAppRunning();
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Launcher stopped");
        
        return Task.CompletedTask;
    }

    #region Private methods

    private void StopApp()
    {
        _appService.StopAppRunning();
    }

    #endregion
}