namespace TradeHero.EntryPoint.Menu.Telegram.Models;

internal class ConnectionTempData
{
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    
    public void ClearData()
    {
        ConnectionId = Guid.Empty;
        ConnectionName = string.Empty;
        ApiKey = string.Empty;
        SecretKey = string.Empty;
    }
}