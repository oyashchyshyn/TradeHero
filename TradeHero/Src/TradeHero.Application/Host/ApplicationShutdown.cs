using TradeHero.Core.Enums;

namespace TradeHero.Application.Host;

internal class ApplicationShutdown
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    private Func<Task>? _actionsBeforeStopApplication;

    public ApplicationShutdown(CancellationTokenSource cancellationTokenSource)
    {
        _cancellationTokenSource = cancellationTokenSource;
    }
    
    public void SetActionsBeforeStop(Func<Task> actionsBeforeStopApplication)
    {
        _actionsBeforeStopApplication = actionsBeforeStopApplication;
    }
    
    public async Task ShutdownAsync(AppExitCode? appExitCode = null)
    {
        if (_actionsBeforeStopApplication != null)
        {
            await _actionsBeforeStopApplication.Invoke();   
        }

        if (appExitCode.HasValue)
        {
            Environment.ExitCode = (int)appExitCode.Value;
        }
        
        _cancellationTokenSource.Cancel();
    }
}