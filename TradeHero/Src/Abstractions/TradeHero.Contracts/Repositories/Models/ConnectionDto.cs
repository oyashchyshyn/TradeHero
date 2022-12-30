namespace TradeHero.Contracts.Repositories.Models;

public class ConnectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}