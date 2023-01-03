using Newtonsoft.Json;

namespace TradeHero.Host.Data.Dtos.Base;

internal abstract class BaseStrategyDto
{
    [JsonIgnore]
    public Guid Id { get; set; }

    public virtual string Name { get; set; } = string.Empty;
}