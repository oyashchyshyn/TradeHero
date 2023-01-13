using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Services;

namespace TradeHero.Services.Services;

internal class ApplicationService : IApplicationService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private Func<Task>? _actionsBeforeStopApplication;
    
    public ApplicationService(
        IHostApplicationLifetime hostApplicationLifetime
        )
    {
        _hostApplicationLifetime = hostApplicationLifetime;
    }
    
    public void SetActionsBeforeStopApplication(Func<Task> actionBeforeStopApplication)
    {
        _actionsBeforeStopApplication = actionBeforeStopApplication;
    }
    
    public async Task StopApplicationAsync()
    {
        if (_actionsBeforeStopApplication != null)
        {
            await _actionsBeforeStopApplication();
        }

        _hostApplicationLifetime.StopApplication();
    }
}