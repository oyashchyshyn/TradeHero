using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TradeHero.Launcher.Host;

internal class LauncherHostedLifeTime : IHostLifetime, IDisposable
{
    private readonly ILogger<LauncherHostedLifeTime> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private readonly ManualResetEvent _shutdownBlock = new(false);
    
    public LauncherHostedLifeTime(
        ILogger<LauncherHostedLifeTime> logger,
        IHostApplicationLifetime hostApplicationLifetime
        )
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
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
        
        _logger.LogInformation("Finish disposing. In {Method}", nameof(Dispose));
    }

    #region Private methods

    private void OnProcessExit(object? sender, EventArgs e)
    {
        _logger.LogInformation("Exit button is pressed. In {Method}", nameof(OnCancelKeyPress));
        
        _hostApplicationLifetime.StopApplication();

        _shutdownBlock.WaitOne();
        
        Environment.ExitCode = 0;
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        _logger.LogInformation("Ctrl + C is pressed. In {Method}", nameof(OnCancelKeyPress));
        
        e.Cancel = true;

        _hostApplicationLifetime.StopApplication();
    }

    #endregion
}