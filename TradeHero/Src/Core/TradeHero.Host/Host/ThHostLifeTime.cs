using Microsoft.Extensions.Hosting;

namespace TradeHero.Host.Host;

internal class ThHostLifeTime : IHostLifetime, IDisposable
{
    private readonly ManualResetEvent _shutdownBlock = new(false);

    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public ThHostLifeTime(IHostApplicationLifetime hostApplicationLifetime)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
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
    }

    #region Private methods

    private void OnProcessExit(object? sender, EventArgs e)
    {
        _hostApplicationLifetime.StopApplication();

        _shutdownBlock.WaitOne();
        
        Environment.ExitCode = 0;
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;

        _hostApplicationLifetime.StopApplication();
    }

    #endregion
}