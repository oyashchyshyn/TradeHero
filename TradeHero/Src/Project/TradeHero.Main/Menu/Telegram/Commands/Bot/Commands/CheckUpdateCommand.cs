using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Menu.Commands;
using TradeHero.Core.Types.Services;
using TradeHero.Main.Menu.Telegram.Store;

namespace TradeHero.Main.Menu.Telegram.Commands.Bot.Commands;

internal class CheckUpdateCommand : ITelegramMenuCommand
{
    private readonly ILogger<CheckUpdateCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IApplicationService _applicationService;
    private readonly IGithubService _githubService;
    private readonly IStoreService _storeService;
    private readonly IEnvironmentService _environmentService;
    private readonly TelegramMenuStore _telegramMenuStore;

    public CheckUpdateCommand(
        ILogger<CheckUpdateCommand> logger,
        ITelegramService telegramService,
        IApplicationService applicationService,
        IGithubService githubService,
        IStoreService storeService,
        IEnvironmentService environmentService,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _applicationService = applicationService;
        _githubService = githubService;
        _storeService = storeService;
        _environmentService = environmentService;
        _telegramMenuStore = telegramMenuStore;
    }

    public string Id => _telegramMenuStore.TelegramButtons.CheckUpdate;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;

            var latestReleaseResult = await _githubService.GetLatestReleaseAsync();
            if (latestReleaseResult.ActionResult != ActionResult.Success)
            {
                await _telegramService.SendTextMessageToUserAsync(
                    "Cannot get information about updates.", 
                    _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Bot),
                    cancellationToken: cancellationToken
                );
                
                return;
            }

            if (!latestReleaseResult.Data.IsNewAvailable)
            {
                await _telegramService.SendTextMessageToUserAsync(
                    "You have latest bot version.", 
                    _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Bot),
                    cancellationToken: cancellationToken
                );
                
                return;
            }
            
            await _telegramService.SendTextMessageToUserAsync(
                $"There is available new version of bot (<b>v{latestReleaseResult.Data.Version.ToString(3)}</b>)", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Bot),
                cancellationToken: cancellationToken
            );
            
            var inlineKeyboardButtons = Enum.GetNames<YesNo>()
                .Select(yesNo => new List<InlineKeyboardButton>
                    {
                        new(yesNo)
                        {
                            CallbackData = yesNo
                        }
                    }
                );

            _telegramMenuStore.CheckUpdateData.ReleaseVersion = latestReleaseResult.Data;
            
            await _telegramService.SendTextMessageToUserAsync(
                $"Do you want to install update?{Environment.NewLine}<b>Tip:</b> Before continue update, please, stop bot trading.", 
                _telegramMenuStore.GetInlineKeyboard(inlineKeyboardButtons),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ExecuteAsync));

            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
        }
    }

    public Task HandleIncomeDataAsync(string data, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public async Task HandleCallbackDataAsync(string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            if (Enum.TryParse(callbackData, out YesNo yesNo))
            {
                if (yesNo == YesNo.No)
                {
                    await SendMessageWithClearDataAsync("You decline update installation.", cancellationToken);
                    
                    return;
                }
                
                await _telegramService.SendTextMessageToUserAsync(
                    "Prepare for update.",
                    _telegramMenuStore.GetRemoveKeyboard(),
                    cancellationToken: cancellationToken
                );
                
                if (_storeService.Bot.TradeLogicStatus == TradeLogicStatus.Running)
                {
                    await SendMessageWithClearDataAsync("You cannot perform update when bot is running. Please, first of all stop bot trading.", cancellationToken);

                    return;
                }
                
                if (_telegramMenuStore.CheckUpdateData.ReleaseVersion == null)
                {
                    _logger.LogError("{PropertyName} is null. In {Method}", 
                        nameof(_telegramMenuStore.CheckUpdateData.ReleaseVersion), nameof(HandleCallbackDataAsync));
                    
                    await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);

                    return;
                }

                var downloadingProgressMessageId = 0;
                var previousProgress = 0.0m;
                _githubService.OnDownloadProgress += async (_, progress) =>
                {
                    if (progress < previousProgress + 120)
                    {
                        return;
                    }
                    
                    if (downloadingProgressMessageId == 0)
                    {
                        var newProgressMessage = await _telegramService.SendTextMessageToUserAsync(
                            $"Downloading progress is: {Math.Round(progress, 0)}%",
                            cancellationToken: cancellationToken
                        );
                    
                        downloadingProgressMessageId = newProgressMessage.Data.MessageId;
                        
                        return;
                    }

                    previousProgress = progress;
                        
                    await _telegramService.EditTextMessageForUserAsync(
                        downloadingProgressMessageId,
                        $"Downloading progress is: {Math.Round(progress, 0)}%",
                        cancellationToken
                    );
                };

                var downloadedAppPath = Path.Combine(_environmentService.GetBasePath(),
                    _telegramMenuStore.CheckUpdateData.ReleaseVersion.AppName);
                
                _logger.LogInformation("Download app path: {DownloadAppPath}. In {Method}", 
                    downloadedAppPath, nameof(HandleCallbackDataAsync));
                
                var downloadResult = await _githubService.DownloadReleaseAsync(
                    _telegramMenuStore.CheckUpdateData.ReleaseVersion.AppDownloadUri, 
                    downloadedAppPath,
                    cancellationToken
                );
                
                if (downloadResult.ActionResult != ActionResult.Success)
                {
                    await SendMessageWithClearDataAsync("There was an error during update, please, check logs.", cancellationToken);
                    
                    return;
                }
                
                _logger.LogInformation("App downloaded. In {Method}", nameof(HandleCallbackDataAsync));
                
                await _telegramService.SendTextMessageToUserAsync(
                    "Update downloaded. Prepare for installing.",
                    cancellationToken: cancellationToken
                );

                _storeService.Application.Update.IsNeedToUpdateApplication = true;
                
                _applicationService.StopApplication(AppExitCode.Update);
                
                return;
            }

            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(HandleCallbackDataAsync));

            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
        }
    }
    
    #region Private methods
    
    private async Task SendMessageWithClearDataAsync(string message, CancellationToken cancellationToken)
    {
        _telegramMenuStore.ClearData();
        
        await _telegramService.SendTextMessageToUserAsync(
            message,
            _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Bot),
            cancellationToken: cancellationToken
        );
    }

    #endregion
}