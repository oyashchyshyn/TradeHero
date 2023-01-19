namespace TradeHero.Core.Types.Trading.Models.Instance;

public class InstanceFactoryResponse
{
    public IInstance Instance { get; init; } = null!;
    public Type Type { get; init; } = null!;
}