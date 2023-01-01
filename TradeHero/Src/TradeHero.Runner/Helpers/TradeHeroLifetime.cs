using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.EntryPoint;

namespace TradeHero.Runner.Helpers;

internal class TradeHeroLifetime : IHostLifetime, IDisposable
{
    private readonly ManualResetEvent _shutdownBlock = new(false);
    private CancellationTokenRegistration _applicationStartedRegistration;
    private CancellationTokenRegistration _applicationStoppingRegistration;

    private readonly IStartup _startup;
    private IHostEnvironment HostEnvironment { get; }
    private IHostApplicationLifetime ApplicationLifetime { get; }
    private ILogger Logger { get; }

    public TradeHeroLifetime(
        IHostEnvironment hostEnvironment, 
        IHostApplicationLifetime applicationLifetime, 
        ILoggerFactory loggerFactory,
        IStartup startup
        )
    {
        HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        Logger = loggerFactory.CreateLogger("TradeHero");

        _startup = startup;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        _applicationStartedRegistration = ApplicationLifetime.ApplicationStarted.Register(OnApplicationStarted);
        _applicationStoppingRegistration = ApplicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Console.CancelKeyPress += OnCancelKeyPress;
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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
        Logger.LogInformation("Application started. Press Ctrl+C to shut down");
        Logger.LogInformation("Hosting environment: {EnvName}", HostEnvironment.EnvironmentName);
        Logger.LogInformation("Content root path: {ContentRoot}", HostEnvironment.ContentRootPath);
        
        await _startup.StartAsync();
    }

    private void OnApplicationStopping()
    {
        Logger.LogInformation("Application is shutting down...");
    }

    private async void OnProcessExit(object? sender, EventArgs e)
    {
        await _startup.EndAsync();
        
        ApplicationLifetime.StopApplication();
        _shutdownBlock.WaitOne();
        Environment.ExitCode = 0;
    }

    private async void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;

        await _startup.EndAsync();
        
        ApplicationLifetime.StopApplication();
    }

    #endregion
}