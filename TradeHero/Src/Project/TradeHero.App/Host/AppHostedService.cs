using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;

namespace TradeHero.App.Host;

internal class AppHostedService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IJobService _jobService;
    private readonly IInternetConnectionService _internetConnectionService;
    private readonly IFileService _fileService;
    private readonly IEnvironmentService _environmentService;
    private readonly IMenuFactory _menuFactory;

    private CancellationTokenSource _cancellationTokenSource = new();
    
    public AppHostedService(
        ILoggerFactory loggerFactory,
        IJobService jobService,
        IInternetConnectionService internetConnectionService,
        IFileService fileService,
        IEnvironmentService environmentService,
        IMenuFactory menuFactory
        )
    {
        _logger = loggerFactory.CreateLogger("TradeHero.App");
        _jobService = jobService;
        _internetConnectionService = internetConnectionService;
        _fileService = fileService;
        _environmentService = environmentService;

        _menuFactory = menuFactory;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("App started");
            _logger.LogInformation("Process id: {ProcessId}", _environmentService.GetCurrentProcessId());
            _logger.LogInformation("Application environment: {GetEnvironmentType}", _environmentService.GetEnvironmentType());
            _logger.LogInformation("Base path: {GetBasePath}", _environmentService.GetBasePath());
            _logger.LogInformation("Runner type: {RunnerType}", _environmentService.GetRunnerType());
            
            if (_environmentService.GetEnvironmentType() == EnvironmentType.Development)
            {
                _logger.LogInformation("Args: {GetBasePath}", string.Join(", ", _environmentService.GetEnvironmentArgs()));   
            }

            await _internetConnectionService.StartInternetConnectionCheckAsync();

            _internetConnectionService.OnInternetConnected += InternetConnectionServiceOnOnInternetConnected;
            _internetConnectionService.OnInternetDisconnected += InternetConnectionServiceOnOnInternetDisconnected;

            RegisterBackgroundJobs();

            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.InitAsync(_cancellationTokenSource.Token);   
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StartAsync));
            
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.FinishAsync(_cancellationTokenSource.Token);
            }
        
            _jobService.FinishAllJobs();

            _internetConnectionService.OnInternetConnected -= InternetConnectionServiceOnOnInternetConnected;
            _internetConnectionService.OnInternetDisconnected -= InternetConnectionServiceOnOnInternetDisconnected;
        
            _internetConnectionService.StopInternetConnectionChecking();
        
            _logger.LogInformation("App stopped");
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StopAsync));
            
            throw;
        }
    }

    #region Private methods

    private void RegisterBackgroundJobs()
    {
        var appSettings = _environmentService.GetAppSettings();
        
        async Task DeleteOldFilesFunction()
        {
            await _fileService.DeleteFilesInFolderAsync(
                Path.Combine(_environmentService.GetBasePath(), appSettings.Folder.LogsFolderName), 
                TimeSpan.FromDays(30).TotalMilliseconds
            );
        }
        
        _jobService.StartJob("DeleteOldLogFiles", DeleteOldFilesFunction, TimeSpan.FromDays(1), true);
    }
    
    private async void InternetConnectionServiceOnOnInternetConnected(object? sender, EventArgs e)
    {
        _cancellationTokenSource = new CancellationTokenSource();

        foreach (var menu in _menuFactory.GetMenus())
        {
            await menu.OnReconnectToInternetAsync(_cancellationTokenSource.Token);
        }
    }
    
    private async void InternetConnectionServiceOnOnInternetDisconnected(object? sender, EventArgs e)
    {
        _cancellationTokenSource.Cancel();

        foreach (var menu in _menuFactory.GetMenus())
        {
            await menu.OnDisconnectFromInternetAsync(_cancellationTokenSource.Token);
        }
    }

    #endregion
}