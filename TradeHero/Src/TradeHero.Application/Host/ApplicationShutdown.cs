using Microsoft.Extensions.Logging;
using TradeHero.Application.Bot;
using TradeHero.Core.Contracts.Client;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;

namespace TradeHero.Application.Host;

internal class ApplicationShutdown
{
    private readonly ILogger<ApplicationShutdown> _logger;
    private readonly IThSocketBinanceClient _socketBinanceClient;
    private readonly IInternetConnectionService _internetConnectionService;
    private readonly IJobService _jobService;
    private readonly BotWorker _botWorker;

    private readonly CancellationTokenSource _cancellationTokenSource;

    public ApplicationShutdown(
        ILogger<ApplicationShutdown> logger,
        IThSocketBinanceClient socketBinanceClient, 
        IInternetConnectionService internetConnectionService, 
        IJobService jobService,
        BotWorker botWorker,
        CancellationTokenSource cancellationTokenSource
        )
    {
        _logger = logger;
        _socketBinanceClient = socketBinanceClient;
        _internetConnectionService = internetConnectionService;
        _jobService = jobService;
        _botWorker = botWorker;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async Task ShutdownAsync(AppExitCode? appExitCode = null)
    {
        try
        {
            if (appExitCode.HasValue)
            {
                Environment.ExitCode = (int)appExitCode.Value;
            }

            await StopServicesAsync();
            
            _cancellationTokenSource.Cancel();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ShutdownAsync));
        }
    }

    #region Private methods

    private async Task StopServicesAsync()
    {
        try
        {
            await _botWorker.FinishBotAsync();
        
            _jobService.FinishAllJobs();

            await _socketBinanceClient.UnsubscribeAllAsync();

            _internetConnectionService.StopInternetConnectionChecking();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StopServicesAsync));
        }
    }

    #endregion
}