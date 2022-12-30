namespace TradeHero.Database.Entities;

internal class User
{
    public User()
    {
        Id = Guid.NewGuid();
    }
    
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long TelegramUserId { get; set; }
    public string TelegramBotToken { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}