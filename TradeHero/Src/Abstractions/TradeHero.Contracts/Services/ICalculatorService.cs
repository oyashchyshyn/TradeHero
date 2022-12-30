using Binance.Net.Enums;
using TradeHero.Contracts.Services.Models.Calculator;

namespace TradeHero.Contracts.Services;

public interface ICalculatorService
{
    decimal CalculatePnl(PositionSide side, decimal lastPrice, decimal entryPrice, decimal quantity);
    public decimal CalculateRoe(PositionSide positionSide, decimal entryPrice, decimal lastPrice, decimal leverage);
    decimal CalculateOrderQuantity(CalculatedOrderQuantity calculatedOrderQuantity);
    decimal RoundToSize(decimal value, decimal size);
    decimal GetOrderQuantity(decimal price, decimal initialMargin, decimal minQuantity);
    decimal GetPercentBetweenTwoPrices(decimal lowerPrice, decimal higherPrice);
    decimal GetPriceFromPercent(decimal lastPrice, decimal percentOfMove);
    decimal GetAvailableBalancePercentWithFutureMargin(decimal balance, decimal availableBalance, decimal futureInitialMargin);
    decimal GetVolatility(decimal highPrice, decimal lowPrice);
    IEnumerable<int> GetIterationValues(int totalValue, int size);
    IEnumerable<double> GetIterationValues(double totalValue, double size);
    IEnumerable<decimal> SplitPositionQuantity(decimal quantity, decimal maxOrderQuantity);
}