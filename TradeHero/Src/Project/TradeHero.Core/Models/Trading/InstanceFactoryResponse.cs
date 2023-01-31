using TradeHero.Core.Contracts.Trading;

namespace TradeHero.Core.Models.Trading;

public class InstanceFactoryResponse
{
    public IInstance Instance { get; init; } = null!;
    public Type Type { get; init; } = null!;
}