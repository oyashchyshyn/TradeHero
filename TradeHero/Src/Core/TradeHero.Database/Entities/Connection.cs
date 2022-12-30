namespace TradeHero.Database.Entities;

internal class Connection
{
    public Connection()
    {
        Id = Guid.NewGuid();
    }
    
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}