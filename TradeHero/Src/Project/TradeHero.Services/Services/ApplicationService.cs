using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Services;

namespace TradeHero.Services.Services;

internal class ApplicationService : IApplicationService
{
    private readonly ILogger<ApplicationService> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private Action? _actionsBeforeStopApplication;
    
    public ApplicationService(
        ILogger<ApplicationService> logger,
        IHostApplicationLifetime hostApplicationLifetime
        )
    {
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
    }
    
    public void SetActionsBeforeStopApplication(Action actionsBeforeStopApplication)
    {
        _actionsBeforeStopApplication = actionsBeforeStopApplication;
    }
    
    public void StopApplication(AppExitCode? appExitCode = null)
    {
        if (_actionsBeforeStopApplication != null)
        {
            _actionsBeforeStopApplication.Invoke();
            
            _logger.LogInformation("Clear resources. In {Method}", nameof(StopApplication));
        }

        _hostApplicationLifetime.StopApplication();

        if (appExitCode.HasValue)
        {
            Environment.ExitCode = (int)appExitCode.Value;
        }
    }
}