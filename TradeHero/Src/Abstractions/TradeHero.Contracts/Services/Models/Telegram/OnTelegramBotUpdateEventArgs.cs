using Telegram.Bot.Types;

namespace TradeHero.Contracts.Services.Models.Telegram;

public class OnTelegramBotUpdateEventArgs : EventArgs
{
    public CallbackQuery? CallbackQuery { get; }
    public Message? Message { get; }
    public CancellationToken CancellationToken { get; }

    public OnTelegramBotUpdateEventArgs(CallbackQuery? callbackQuery, Message? message, CancellationToken cancellationToken)
    {
        CallbackQuery = callbackQuery;
        Message = message;
        CancellationToken = cancellationToken;
    }
}