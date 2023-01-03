using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Telegram;

namespace TradeHero.Core.Services;

internal class TelegramService : ITelegramService
{
    private readonly ILogger<TelegramService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserRepository _userRepository;

    private ITelegramBotClient? _telegramBotClient;
    private long _userId;
    
    public event EventHandler<OnTelegramBotUpdateEventArgs>? OnTelegramBotUserChatUpdate;

    public TelegramService(
        ILogger<TelegramService> logger,
        IServiceProvider serviceProvider,
        IUserRepository userRepository
        )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _userRepository = userRepository;
    }

    public async Task<ActionResult> InitAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_userId == 0)
            {
                var activeUser = await _userRepository.GetActiveUserAsync();
                if (activeUser == null)
                {
                    _logger.LogError("There is no active user. In {Method}", nameof(InitAsync));

                    return ActionResult.Error;
                }
                
                _userId = activeUser.TelegramUserId;   
            }

            _telegramBotClient ??= _serviceProvider.GetRequiredService<ITelegramBotClient>();

            var testApiResult = await _telegramBotClient.TestApiAsync(cancellationToken);
            if (!testApiResult)
            {
                _logger.LogError("Cannot connect to telegram servers. In {Method}",
                    nameof(InitAsync));

                return ActionResult.ClientError;
            }

            var receiverOptions = new ReceiverOptions
            {
                Limit = limit,
                ThrowPendingUpdates = true
            };
            
            _telegramBotClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions, 
                cancellationToken: cancellationToken
            );

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(InitAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(InitAsync));

            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> TestBotConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_telegramBotClient == null)
            {
                _logger.LogError("{Instance} is null. In {Method}", 
                    nameof(_telegramBotClient), nameof(TestBotConnectionAsync));
                
                return ActionResult.Error;
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(TestBotConnectionAsync));
                
                return ActionResult.CancellationTokenRequested;
            }
            
            var result = await _telegramBotClient.TestApiAsync(cancellationToken);
            return !result ? ActionResult.ClientError : ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(TestBotConnectionAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(TestBotConnectionAsync));
            
            return ActionResult.SystemError;
        }
    }

    public async Task<GenericBaseResult<Chat>> GetBotChat(long chatId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_telegramBotClient == null)
            {
                _logger.LogError("{Instance} is null. In {Method}", 
                    nameof(_telegramBotClient), nameof(ChangeChannelTitleAsync));
                
                return new GenericBaseResult<Chat>(ActionResult.Error);
            }
        
            if (chatId == 0)
            {
                _logger.LogError("User chat id cannot be equal to zero. In {Method}", 
                    nameof(ChangeChannelTitleAsync));
                
                return new GenericBaseResult<Chat>(ActionResult.Error);
            }
        
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(ChangeChannelTitleAsync));
                
                return new GenericBaseResult<Chat>(ActionResult.CancellationTokenRequested);
            }
        
            var channel = await _telegramBotClient.GetChatAsync(
                chatId, 
                cancellationToken: cancellationToken
            );

            return new GenericBaseResult<Chat>(channel);
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(ChangeChannelTitleAsync));
            
            return new GenericBaseResult<Chat>(ActionResult.CancellationTokenRequested);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ChangeChannelTitleAsync));
            
            return new GenericBaseResult<Chat>(ActionResult.SystemError);
        }
    }

    public async Task<ActionResult> ChangeChannelTitleAsync(long channelId, string title, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_telegramBotClient == null)
            {
                _logger.LogError("{Instance} is null. In {Method}", 
                    nameof(_telegramBotClient), nameof(ChangeChannelTitleAsync));
                
                return ActionResult.Error;
            }
        
            if (channelId == 0)
            {
                _logger.LogError("User chat id cannot be equal to zero. In {Method}", 
                    nameof(ChangeChannelTitleAsync));
                
                return ActionResult.Error;
            }

            if (string.IsNullOrEmpty(title))
            {
                _logger.LogError("Title cannot be null or empty. In {Method}", 
                    nameof(ChangeChannelTitleAsync));
                
                return ActionResult.Error;
            }

            if (title.Length > TelegramConstants.MaximumChannelTitleLenght)
            {
                _logger.LogError("Title length cannot be greater then {MaximumChannelTitleLenght}. In {Method}", 
                    TelegramConstants.MaximumChannelTitleLenght, nameof(ChangeChannelTitleAsync));
                
                return ActionResult.Error;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(ChangeChannelTitleAsync));
                
                return ActionResult.CancellationTokenRequested;
            }
            
            var channel = await _telegramBotClient.GetChatAsync(
                channelId, 
                cancellationToken: cancellationToken
            );

            if (channel.Title == title)
            {
                _logger.LogInformation("Title do not need to change. In {Method}", 
                    nameof(SendTextMessageToChannelAsync));
                
                return ActionResult.Success;
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(ChangeChannelTitleAsync));
                
                return ActionResult.CancellationTokenRequested;
            }

            await _telegramBotClient.SetChatTitleAsync(channelId, title, cancellationToken);

            _logger.LogInformation("Title changed to {NewTitle} in channel {ChannelId}. In {Method}", 
                title, channelId, nameof(SendTextMessageToChannelAsync));
            
            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(ChangeChannelTitleAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ChangeChannelTitleAsync));
            
            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> SendTextMessageToChannelAsync(long channelId, string text, bool? disableNotification = false, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_telegramBotClient == null)
            {
                _logger.LogError("{Instance} is null. In {Method}", 
                    nameof(_telegramBotClient), nameof(SendTextMessageToChannelAsync));
                
                return ActionResult.Error;
            }
            
            if (channelId == 0)
            {
                _logger.LogError("Channel chat id cannot be equal to zero. In {Method}", 
                    nameof(SendTextMessageToChannelAsync));
                
                return ActionResult.Error;
            }

            if (string.IsNullOrEmpty(text))
            {
                _logger.LogError("Text cannot be null or empty. In {Method}", 
                    nameof(SendTextMessageToChannelAsync));
                
                return ActionResult.Error;
            }

            if (text.Length > TelegramConstants.MaximumMessageLenght)
            {
                _logger.LogError("Text message length cannot be greater then {MaximumMessageLenght}. In {Method}", 
                    TelegramConstants.MaximumMessageLenght, nameof(SendTextMessageToChannelAsync));
                
                return ActionResult.Error;
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(SendTextMessageToChannelAsync));
                
                return ActionResult.CancellationTokenRequested;
            }
            
            await _telegramBotClient.SendTextMessageAsync(
                channelId,
                text,
                parseMode: ParseMode.Html,
                disableNotification: disableNotification,
                cancellationToken: cancellationToken
            );

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SendTextMessageToChannelAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SendTextMessageToChannelAsync));
            
            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> SendTextMessageToUserAsync(string text, ReplyMarkupBase replyMarkupBase, 
        bool? disableNotification = false, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_telegramBotClient == null)
            {
                _logger.LogError("{Instance} is null. In {Method}", 
                    nameof(_telegramBotClient), nameof(SendTextMessageToUserAsync));
                
                return ActionResult.Error;
            }

            if (string.IsNullOrEmpty(text))
            {
                _logger.LogError("Text cannot be null or empty. In {Method}", 
                    nameof(SendTextMessageToUserAsync));
                
                return ActionResult.Error;
            }

            if (text.Length > TelegramConstants.MaximumMessageLenght)
            {
                _logger.LogError("Text message length cannot be greater then {MaximumMessageLenght}. In {Method}", 
                    TelegramConstants.MaximumMessageLenght, nameof(SendTextMessageToUserAsync));
                
                return ActionResult.Error;
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(SendTextMessageToUserAsync));
                
                return ActionResult.CancellationTokenRequested;
            }
            
            await _telegramBotClient.SendTextMessageAsync(
                _userId,
                text,
                parseMode: ParseMode.Html,
                disableNotification: disableNotification,
                cancellationToken: cancellationToken,
                replyMarkup: replyMarkupBase
            );

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SendTextMessageToUserAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SendTextMessageToUserAsync));
            
            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> SendTextMessageToUserAsync(string text, InlineKeyboardMarkup inlineKeyboardMarkup, bool? disableNotification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_telegramBotClient == null)
            {
                _logger.LogError("{Instance} is null. In {Method}", 
                    nameof(_telegramBotClient), nameof(SendTextMessageToUserAsync));
                
                return ActionResult.Error;
            }

            if (string.IsNullOrEmpty(text))
            {
                _logger.LogError("Text cannot be null or empty. In {Method}", 
                    nameof(SendTextMessageToUserAsync));
                
                return ActionResult.Error;
            }

            if (text.Length > TelegramConstants.MaximumMessageLenght)
            {
                _logger.LogError("Text message length cannot be greater then {MaximumMessageLenght}. In {Method}", 
                    TelegramConstants.MaximumMessageLenght, nameof(SendTextMessageToUserAsync));
                
                return ActionResult.Error;
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(SendTextMessageToUserAsync));
                
                return ActionResult.CancellationTokenRequested;
            }
            
            await _telegramBotClient.SendTextMessageAsync(
                _userId,
                text,
                parseMode: ParseMode.Html,
                disableNotification: disableNotification,
                cancellationToken: cancellationToken,
                replyMarkup: inlineKeyboardMarkup
            );

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SendTextMessageToUserAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SendTextMessageToUserAsync));
            
            return ActionResult.SystemError;
        }
    }
    
    public async Task<ActionResult> SendTextMessageToUserAsync(string text, bool? disableNotification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_telegramBotClient == null)
            {
                _logger.LogError("{Instance} is null. In {Method}", 
                    nameof(_telegramBotClient), nameof(SendTextMessageToUserAsync));
                
                return ActionResult.Error;
            }

            if (string.IsNullOrEmpty(text))
            {
                _logger.LogError("Text cannot be null or empty. In {Method}", 
                    nameof(SendTextMessageToUserAsync));
                
                return ActionResult.Error;
            }

            if (text.Length > TelegramConstants.MaximumMessageLenght)
            {
                _logger.LogError("Text message length cannot be greater then {MaximumMessageLenght}. In {Method}", 
                    TelegramConstants.MaximumMessageLenght, nameof(SendTextMessageToUserAsync));
                
                return ActionResult.Error;
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(SendTextMessageToUserAsync));
                
                return ActionResult.CancellationTokenRequested;
            }
            
            await _telegramBotClient.SendTextMessageAsync(
                _userId,
                text,
                parseMode: ParseMode.Html,
                disableNotification: disableNotification,
                cancellationToken: cancellationToken
            );

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SendTextMessageToUserAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SendTextMessageToUserAsync));
            
            return ActionResult.SystemError;
        }
    }

    public Task<ActionResult> CloseConnectionAsync()
    {
        try
        {
            _telegramBotClient = null;
            
            return Task.FromResult(ActionResult.Success);
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(CloseConnectionAsync));
            
            return Task.FromResult(ActionResult.CancellationTokenRequested);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(CloseConnectionAsync));
            
            return Task.FromResult(ActionResult.SystemError);
        }
    }
    
    #region Private method

    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogWarning(exception, "In {Method}", nameof(HandleErrorAsync));

        return Task.CompletedTask;
    }

    private Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(HandleUpdateAsync));
                
                return Task.CompletedTask;
            }
            
            if (update.Message != null && update.Message.Chat.Id != _userId)
            {
                _logger.LogWarning("Message chat id {ChatId} does not equal to user chat id {UserChatId}. In {Method}", 
                    update.Message?.Chat.Id, _userId, nameof(HandleUpdateAsync));

                return Task.CompletedTask;
            }

            OnTelegramBotUserChatUpdate?.Invoke(this, new OnTelegramBotUpdateEventArgs(
                update.CallbackQuery, 
                update.Message, 
                cancellationToken)
            );

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(HandleUpdateAsync));

            return Task.CompletedTask;
        }
    }

    #endregion
}