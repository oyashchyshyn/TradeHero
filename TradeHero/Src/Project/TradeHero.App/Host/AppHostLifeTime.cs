using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TradeHero.App.Host;

internal class AppHostLifeTime : IHostLifetime, IDisposable
{
    private readonly ILogger<AppHostLifeTime> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private readonly ManualResetEvent _shutdownBlock = new(false);
    
    public AppHostLifeTime(
        ILogger<AppHostLifeTime> logger,
        IHostApplicationLifetime hostApplicationLifetime
        )
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

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

        _logger.LogInformation("Finish disposing. In {Method}", nameof(Dispose));
    }

    #region Private methods

    private void OnProcessExit(object? sender, EventArgs e)
    {
        _logger.LogInformation("Exit button is pressed. In {Method}", nameof(OnProcessExit));
        
        _hostApplicationLifetime.StopApplication();

        _shutdownBlock.WaitOne();
        
        Environment.ExitCode = 0;
    }

    #endregion
}