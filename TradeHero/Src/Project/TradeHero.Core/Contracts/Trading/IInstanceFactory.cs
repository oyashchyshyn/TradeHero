using TradeHero.Core.Enums;
using TradeHero.Core.Models.Trading;

namespace TradeHero.Core.Contracts.Trading;

public interface IInstanceFactory
{
    InstanceFactoryResponse? GetInstance(InstanceType instanceType);
}