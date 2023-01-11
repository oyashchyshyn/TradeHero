using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Menu.Commands;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Host.Menu.Telegram.Store;

namespace TradeHero.Host.Menu.Telegram.Commands.Bot.Commands;

internal class CheckUpdateCommand : ITelegramMenuCommand
{
    private readonly ILogger<CheckUpdateCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IGithubService _githubService;
    private readonly IStore _store;
    private readonly IEnvironmentService _environmentService;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly TelegramMenuStore _telegramMenuStore;

    public CheckUpdateCommand(
        ILogger<CheckUpdateCommand> logger,
        ITelegramService telegramService,
        IGithubService githubService,
        IStore store,
        IEnvironmentService environmentService,
        IHostApplicationLifetime hostApplicationLifetime,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _githubService = githubService;
        _store = store;
        _environmentService = environmentService;
        _hostApplicationLifetime = hostApplicationLifetime;
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
                
                await _telegramService.SendTextMessageToUserAsync(
                    "Update downloaded. Prepare for installing.",
                    cancellationToken: cancellationToken
                );

                _environmentService.CustomArgs.Clear();
                _environmentService.CustomArgs.Add(ArgumentKeyConstants.Update, string.Empty);

                _hostApplicationLifetime.StopApplication();
                
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