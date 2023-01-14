using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Sockets;
using TradeHero.Core.Enums;
using TradeHero.Launcher.Services;

namespace TradeHero.Launcher.Host;

internal class LauncherHostedService : IHostedService
{
    private readonly ILogger<LauncherHostedService> _logger;
    private readonly IApplicationService _applicationService;
    private readonly IEnvironmentService _environmentService;
    private readonly IServerSocket _serverSocket;

    private readonly AppService _appService;
    
    public LauncherHostedService(
        ILogger<LauncherHostedService> logger, 
        IApplicationService applicationService,
        IEnvironmentService environmentService, 
        IServerSocket serverSocket, 
        AppService appService
        )
    {
        _logger = logger;
        _environmentService = environmentService;
        _applicationService = applicationService;
        _serverSocket = serverSocket;
        
        _appService = appService;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Launcher started. Press Ctrl+C to shut down");
        _logger.LogInformation("Process id: {ProcessId}", _environmentService.GetCurrentProcessId());
        _logger.LogInformation("Base path: {GetBasePath}", _environmentService.GetBasePath());
        _logger.LogInformation("Environment: {GetEnvironmentType}", _environmentService.GetEnvironmentType());
        _logger.LogInformation("Runner type: {RunnerType}", _environmentService.GetRunnerType());
        
        _serverSocket.StartListen();
        _applicationService.SetActionsBeforeStopApplication(StopLauncherActions);
        
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

    private void StopLauncherActions()
    {
        _appService.StopAppRunning();
        _serverSocket.Close();
    }

    #endregion
}