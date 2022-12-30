using TradeHero.Contracts.Client;
using TradeHero.Contracts.EntryPoint;
using TradeHero.Contracts.Services;
using TradeHero.EntryPoint.Menu.Telegram;

namespace TradeHero.EntryPoint.Init;

internal class Startup : IStartup
{
    private readonly IThSocketBinanceClient _socketBinanceClient;
    private readonly IJobService _jobService;
    private readonly IInternetConnectionService _internetConnectionService;
    private readonly IFileService _fileService;
    private readonly IEnvironmentService _environmentService;

    private readonly TelegramMenu _telegramMenu;

    private CancellationTokenSource _cancellationTokenSource = new();

    public Startup(
        IThSocketBinanceClient socketBinanceClient,
        IJobService jobService,
        IInternetConnectionService internetConnectionService,
        IFileService fileService,
        TelegramMenu telegramMenu, IEnvironmentService environmentService)
    {
        _socketBinanceClient = socketBinanceClient;
        _jobService = jobService;
        _internetConnectionService = internetConnectionService;
        _fileService = fileService;
        
        _telegramMenu = telegramMenu;
        _environmentService = environmentService;
    }

    public async Task StartAsync()
    {
        await _internetConnectionService.StartInternetConnectionCheckAsync();

        _internetConnectionService.OnInternetConnected += InternetConnectionServiceOnOnInternetConnected;
        _internetConnectionService.OnInternetDisconnected += InternetConnectionServiceOnOnInternetDisconnected;

        RegisterBackgroundJobs();
        
        await _telegramMenu.InitAsync(_cancellationTokenSource.Token);
    }
    
    public async Task EndAsync()
    {
        await _telegramMenu.FinishAsync(_cancellationTokenSource.Token);
        
        _jobService.FinishAllJobs();
        
        await _socketBinanceClient.UnsubscribeAllAsync();

        _internetConnectionService.OnInternetConnected -= InternetConnectionServiceOnOnInternetConnected;
        _internetConnectionService.OnInternetDisconnected -= InternetConnectionServiceOnOnInternetDisconnected;
        
        _internetConnectionService.StopInternetConnectionChecking();
    }

    #region Private methods

    private async void InternetConnectionServiceOnOnInternetConnected(object? sender, EventArgs e)
    {
        _cancellationTokenSource = new CancellationTokenSource();

        await _telegramMenu.OnReconnectToInternetAsync(_cancellationTokenSource.Token);
    }
    
    private async void InternetConnectionServiceOnOnInternetDisconnected(object? sender, EventArgs e)
    {
        _cancellationTokenSource.Cancel();

        await _telegramMenu.OnDisconnectFromInternetAsync();
    }

    private void RegisterBackgroundJobs()
    {
        async Task DeleteOldFilesFunction()
        {
            await _fileService.DeleteFilesInFolderAsync(
                _environmentService.GetLogsFolderPath(), 
                TimeSpan.FromDays(30).TotalMilliseconds
            );
        }
        
        _jobService.StartJob("DeleteOldLogFiles", DeleteOldFilesFunction, TimeSpan.FromDays(1), true);
    }

    #endregion
}