using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.Services.Models.Telegram;

namespace TradeHero.Contracts.Services;

public interface ITelegramService
{
    event EventHandler<OnTelegramBotUpdateEventArgs> OnTelegramBotUserChatUpdate;

    Task<ActionResult> InitAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<ActionResult> TestBotConnectionAsync(CancellationToken cancellationToken = default);
    Task<GenericBaseResult<Chat>> GetBotChat(long chatId, CancellationToken cancellationToken = default);
    Task<ActionResult> ChangeChannelTitleAsync(long channelId, string title, CancellationToken cancellationToken = default);
    Task<GenericBaseResult<Message>> SendTextMessageToChannelAsync(long channelId, string text, bool? disableNotification = false, CancellationToken cancellationToken = default);
    Task<GenericBaseResult<Message>> SendTextMessageToUserAsync(string text, bool? disableNotification = false, CancellationToken cancellationToken = default);
    Task<GenericBaseResult<Message>> SendTextMessageToUserAsync(string text, ReplyMarkupBase replyMarkupBase, bool? disableNotification = false, CancellationToken cancellationToken = default);
    Task<GenericBaseResult<Message>> SendTextMessageToUserAsync(string text, InlineKeyboardMarkup inlineKeyboardMarkup, bool? disableNotification = false, CancellationToken cancellationToken = default);
    Task<GenericBaseResult<Message>> EditTextMessageForUserAsync(int messageId, string text, CancellationToken cancellationToken = default);
    Task<ActionResult> CloseConnectionAsync();
}