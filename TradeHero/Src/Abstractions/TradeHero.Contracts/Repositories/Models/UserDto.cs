namespace TradeHero.Contracts.Repositories.Models;

public class UserDto
{
    public long TelegramUserId { get; set; }
    public string TelegramBotToken { get; set; } = string.Empty;
}