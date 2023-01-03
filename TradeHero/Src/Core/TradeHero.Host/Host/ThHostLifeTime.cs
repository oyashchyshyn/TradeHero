using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Services;
using TradeHero.EntryPoint.Menu;

namespace TradeHero.EntryPoint.Host;

internal class ThHostLifeTime : IHostLifetime, IDisposable
{
    private readonly ManualResetEvent _shutdownBlock = new(false);
    private CancellationTokenRegistration _applicationStartedRegistration;
    private CancellationTokenRegistration _applicationStoppingRegistration;

    private readonly ILogger _logger;
    private readonly IJobService _jobService;
    private readonly IInternetConnectionService _internetConnectionService;
    private readonly IFileService _fileService;
    private readonly IEnvironmentService _environmentService;
    private readonly IHostApplicationLifetime _applicationLifetime;

    private readonly MenuFactory _menuFactory;

    private CancellationTokenSource _cancellationTokenSource = new();
    
    public ThHostLifeTime(
        ILoggerFactory loggerFactory,
        IHostApplicationLifetime applicationLifetime,
        IJobService jobService,
        IInternetConnectionService internetConnectionService,
        IFileService fileService,
        IEnvironmentService environmentService,
        MenuFactory menuFactory
        )
    {
        _logger = loggerFactory.CreateLogger("TradeHero");
        _jobService = jobService;
        _internetConnectionService = internetConnectionService;
        _fileService = fileService;
        _environmentService = environmentService;
        _applicationLifetime = applicationLifetime;

        _menuFactory = menuFactory;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        _applicationStartedRegistration = _applicationLifetime.ApplicationStarted.Register(OnApplicationStarted);
        _applicationStoppingRegistration = _applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Console.CancelKeyPress += OnCancelKeyPress;
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task RestartAsync()
    {
        await EndAsync();
        
        var operationSystem = _environmentService.GetCurrentOperationSystem();
        var applicationPath = Path.Combine(
            _environmentService.GetBasePath(),
            _environmentService.GetApplicationNameByOperationSystem(operationSystem)
        );
        
        _applicationLifetime.StopApplication();

        Process.Start(applicationPath, $"--{ArgumentConstants.UpdateKey}=true");
    }

    public void Dispose()
    {
        _shutdownBlock.Set();

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        Console.CancelKeyPress -= OnCancelKeyPress;

        _applicationStartedRegistration.Dispose();
        _applicationStoppingRegistration.Dispose();
    }

    #region Private methods

    private async void OnApplicationStarted()
    {
        _logger.LogInformation("Application started. Press Ctrl+C to shut down");
        _logger.LogInformation("Application environment: {GetEnvironmentType}", _environmentService.GetEnvironmentType());
        _logger.LogInformation("Base path: {GetBasePath}", _environmentService.GetBasePath());

        await _internetConnectionService.StartInternetConnectionCheckAsync();

        _internetConnectionService.OnInternetConnected += InternetConnectionServiceOnOnInternetConnected;
        _internetConnectionService.OnInternetDisconnected += InternetConnectionServiceOnOnInternetDisconnected;

        RegisterBackgroundJobs();

        foreach (var menu in _menuFactory.GetMenus())
        {
            await menu.InitAsync(_cancellationTokenSource.Token);   
        }
    }

    private void OnApplicationStopping()
    {
        _logger.LogInformation("Application is shutting down...");
    }

    private async void OnProcessExit(object? sender, EventArgs e)
    {
        await EndAsync();
        
        _applicationLifetime.StopApplication();
        _shutdownBlock.WaitOne();
        
        Environment.ExitCode = 0;
    }

    private async void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;

        await EndAsync();
        
        _applicationLifetime.StopApplication();
    }

    private async Task EndAsync()
    {
        foreach (var menu in _menuFactory.GetMenus())
        {
            await menu.FinishAsync(_cancellationTokenSource.Token);
        }
        
        _jobService.FinishAllJobs();

        _internetConnectionService.OnInternetConnected -= InternetConnectionServiceOnOnInternetConnected;
        _internetConnectionService.OnInternetDisconnected -= InternetConnectionServiceOnOnInternetDisconnected;
        
        _internetConnectionService.StopInternetConnectionChecking();
    }
    
    private void RegisterBackgroundJobs()
    {
        async Task DeleteOldFilesFunction()
        {
            await _fileService.DeleteFilesInFolderAsync(
                _environmentService.GetLogsFolderPath(), 
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