using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Store;
using TradeHero.Host.Host;
using TradeHero.Host.Menu.Telegram.Store;

namespace TradeHero.Host.Menu.Telegram.Commands.Bot.Commands;

internal class CheckUpdateCommand : IMenuCommand
{
    private readonly ILogger<CheckUpdateCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IUpdateService _updateService;
    private readonly IStore _store;
    private readonly IHostLifetime _hostLifetime;
    private readonly IEnvironmentService _environmentService;
    private readonly TelegramMenuStore _telegramMenuStore;

    public CheckUpdateCommand(
        ILogger<CheckUpdateCommand> logger,
        ITelegramService telegramService,
        IUpdateService updateService,
        IStore store,
        IHostLifetime hostLifetime,
        IEnvironmentService environmentService,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _updateService = updateService;
        _store = store;
        _environmentService = environmentService;
        _hostLifetime = hostLifetime;
        _telegramMenuStore = telegramMenuStore;
    }

    public string Id => _telegramMenuStore.TelegramButtons.CheckUpdate;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;

            var latestReleaseResult = await _updateService.GetLatestReleaseAsync();
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
                
                if (_store.Bot.TradeLogicStatus == TradeLogicStatus.Running)
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
                _updateService.OnDownloadProgress += async (_, progress) =>
                {
                    // if (downloadingProgressMessageId == 0)
                    // {
                    //     var newProgressMessage = await _telegramService.SendTextMessageToUserAsync(
                    //         $"Downloading progress is: {Math.Round(progress, 0)}%",
                    //         cancellationToken: cancellationToken
                    //     );
                    //
                    //     downloadingProgressMessageId = newProgressMessage.Data.MessageId;
                    //     
                    //     return;
                    // }
                    //
                    // if (progress <= previousProgress + 5)
                    // {
                    //     return;
                    // }
                    //
                    // previousProgress = progress;
                    //     
                    // await _telegramService.EditTextMessageForUserAsync(
                    //     downloadingProgressMessageId,
                    //     $"Downloading progress is: {Math.Round(progress, 0)}%",
                    //     cancellationToken
                    // );
                };
                
                var downloadResult = await _updateService.UpdateApplicationAsync(
                    _telegramMenuStore.CheckUpdateData.ReleaseVersion, 
                    cancellationToken
                );

                if (downloadResult.ActionResult != ActionResult.Success)
                {
                    await SendMessageWithClearDataAsync("There was an error during update, please, check logs.", cancellationToken);
                    
                    return;
                }
                
                await _telegramService.SendTextMessageToUserAsync(
                    "Update downloaded. Prepare for installing.",
                    cancellationToken: cancellationToken
                );
                
                var args = $"--bfp={_environmentService.GetBasePath()} " +
                           $"--ufp={_environmentService.GetUpdateFolderPath()}" +
                           $"--man={_environmentService.GetCurrentApplicationName()}" +
                           $"--dan={downloadResult.Data.AppFileName}";
                
                await ((ThHostLifeTime)_hostLifetime).RunUpdaterAsync(
                    Path.Combine(_environmentService.GetUpdateFolderPath(), downloadResult.Data.UpdaterFileName),
                    args
                );
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