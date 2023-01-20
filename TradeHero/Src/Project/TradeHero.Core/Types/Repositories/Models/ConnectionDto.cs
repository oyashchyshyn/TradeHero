using Newtonsoft.Json;

namespace TradeHero.Core.Types.Repositories.Models;

public class ConnectionDto
{
    public Guid Id { get; set; }
    
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty("apiKey")]
    public string ApiKey { get; set; } = string.Empty;
    
    [JsonProperty("secretKey")]
    public string SecretKey { get; set; } = string.Empty;
    public DateTime CreationDateTime { get; set; }
    public bool IsActive { get; set; }
}