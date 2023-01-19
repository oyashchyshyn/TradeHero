using Microsoft.Extensions.Hosting;

namespace TradeHero.Launcher.Services;

internal class LauncherHostLifetime : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted { get; }
    public CancellationToken ApplicationStopping { get; }
    public CancellationToken ApplicationStopped { get; }
    
    public LauncherHostLifetime(CancellationToken applicationStarted, CancellationToken applicationStopping, CancellationToken applicationStopped)
    {
        ApplicationStarted = applicationStarted;
        ApplicationStopping = applicationStopping;
        ApplicationStopped = applicationStopped;
    }

    public void StopApplication()
    {
        
    }
}