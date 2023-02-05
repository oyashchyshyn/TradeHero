using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Application.Bot;
using TradeHero.Core.Constants;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;

namespace TradeHero.Application.Host;

internal class AppHostedService : IHostedService
{
    private readonly ILogger<AppHostedService> _logger;
    private readonly IJobService _jobService;
    private readonly IInternetConnectionService _internetConnectionService;
    private readonly IFileService _fileService;
    private readonly IEnvironmentService _environmentService;
    private readonly ApplicationShutdown _applicationShutdown;
    private readonly BotWorker _botWorker;

    public AppHostedService(
        ILogger<AppHostedService> logger,
        IJobService jobService,
        IInternetConnectionService internetConnectionService,
        IFileService fileService,
        IEnvironmentService environmentService,
        ApplicationShutdown applicationShutdown, 
        BotWorker botWorker
        )
    {
        _logger = logger;
        _jobService = jobService;
        _internetConnectionService = internetConnectionService;
        _fileService = fileService;
        _environmentService = environmentService;
        _applicationShutdown = applicationShutdown;
        _botWorker = botWorker;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("App started");
            _logger.LogInformation("Process id: {ProcessId}", _environmentService.GetCurrentProcessId());
            _logger.LogInformation("Application environment: {GetEnvironmentType}", _environmentService.GetEnvironmentType());
            _logger.LogInformation("Base path: {GetBasePath}", _environmentService.GetBasePath());
            _logger.LogInformation("Runner type: {RunnerType}", _environmentService.GetRunnerType());

            if (_environmentService.GetEnvironmentType() == EnvironmentType.Development)
            {
                _logger.LogInformation("Args: {GetBasePath}", string.Join(", ", _environmentService.GetEnvironmentArgs()));   
            }

            await _internetConnectionService.StartInternetConnectionCheckAsync();

            var appSettings = _environmentService.GetAppSettings();
        
            async Task DeleteOldLogFilesFunction()
            {
                await _fileService.DeleteFilesInFolderAsync(
                    Path.Combine(_environmentService.GetBasePath(), appSettings.Folder.LogsFolderName), 
                    TimeSpan.FromDays(30).TotalMilliseconds
                );
            }
        
            _jobService.StartJob("DeleteOldLogFilesFunction", DeleteOldLogFilesFunction, TimeSpan.FromDays(1), true);
        
            async Task DeleteOldClusterResultFilesFunction()
            {
                await _fileService.DeleteFilesInFolderAsync(
                    Path.Combine(_environmentService.GetBasePath(), FolderConstants.ClusterResultsFolder), 
                    TimeSpan.FromDays(10).TotalMilliseconds
                );
            }
        
            _jobService.StartJob("DeleteOldClusterResultFilesFunction", DeleteOldClusterResultFilesFunction, TimeSpan.FromDays(1), true);
            
            await _botWorker.InitBotAsync();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StartAsync));

            await _applicationShutdown.ShutdownAsync(AppExitCode.Failure);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("App stopped");
        
        return Task.CompletedTask;
    }
}