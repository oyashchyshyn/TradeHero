using TradeHero.Core.Enums;

namespace TradeHero.Application.Host;

internal class ApplicationShutdown
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    private Action? _actionsBeforeStopApplication;

    public ApplicationShutdown(CancellationTokenSource cancellationTokenSource)
    {
        _cancellationTokenSource = cancellationTokenSource;
    }
    
    public void SetActionsBeforeStop(Action actionsBeforeStopApplication)
    {
        _actionsBeforeStopApplication = actionsBeforeStopApplication;
    }
    
    public void Shutdown(AppExitCode? appExitCode = null)
    {
        _actionsBeforeStopApplication?.Invoke();

        if (appExitCode.HasValue)
        {
            Environment.ExitCode = (int)appExitCode.Value;
        }
        
        _cancellationTokenSource.Cancel();
    }
}