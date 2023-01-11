using System.Globalization;
using Binance.Net.Enums;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Calculator;

namespace TradeHero.Services.Services;

internal class CalculatorService : ICalculatorService
{
    public decimal CalculateOrderQuantity(CalculatedOrderQuantity calculatedOrderQuantity)
    {
        var futureOrderQuantity = calculatedOrderQuantity.MinOrderSize;
        var initialMargin = calculatedOrderQuantity.EntryPrice * calculatedOrderQuantity.TotalQuantity;
        
        do
        {
            var futurePositionInitialMargin = calculatedOrderQuantity.LastPrice * futureOrderQuantity;
            if (futurePositionInitialMargin < calculatedOrderQuantity.MinNotional)
            {
                futureOrderQuantity += calculatedOrderQuantity.MinOrderSize;
                
                continue;
            }

            var totalQuantity = calculatedOrderQuantity.TotalQuantity + futureOrderQuantity;
            var averagePrice = (initialMargin + futurePositionInitialMargin) / totalQuantity;
            var futureRoe = CalculateRoe(calculatedOrderQuantity.Side, averagePrice, calculatedOrderQuantity.LastPrice, calculatedOrderQuantity.Leverage);
            if (futureRoe >= calculatedOrderQuantity.MinRoePercent)
            {
                return futureOrderQuantity;
            }
                
            futureOrderQuantity += calculatedOrderQuantity.MinOrderSize;
        } 
        while (true);
    }
    
    public decimal CalculatePnl(PositionSide positionSide, decimal lastPrice, decimal entryPrice, decimal quantity)
    {
        var side = positionSide == PositionSide.Short ? -1 : 1;
        
        return quantity * side * (lastPrice - entryPrice);
    }

    public decimal CalculateRoe(PositionSide positionSide, decimal entryPrice, decimal lastPrice, decimal leverage)
    {
        var side = positionSide == PositionSide.Short ? -1 : 1;
        
        return Math.Round((lastPrice - entryPrice) * side * leverage / entryPrice * 100, 2);
    }

    public decimal RoundToSize(decimal value, decimal size)
    {
        var decimalsToLeaveAfterPoint = GetCountDecimalsAfterPoint(size);

        var result = Math.Round(value, decimalsToLeaveAfterPoint);
        
        return result;
    }

    public decimal GetOrderQuantity(decimal price, decimal initialMargin, decimal minQuantity)
    {
        var orderAmount = RoundToSize(initialMargin / price, minQuantity);

        do
        {
            if (orderAmount * price > initialMargin)
            {
                return orderAmount;
            }

            orderAmount += minQuantity;
        } 
        while (true);
    }
    
    public decimal GetPercentBetweenTwoPrices(decimal lowerPrice, decimal higherPrice)
    {
        return Math.Round((higherPrice - lowerPrice) / higherPrice * 100, 2);
    }

    public decimal GetPriceFromPercent(decimal lastPrice, decimal percentOfMove)
    {
        var valueFromPercent = lastPrice * percentOfMove / 100;
        return lastPrice + valueFromPercent;
    }

    public decimal GetAvailableBalancePercentWithFutureMargin(decimal balance, decimal availableBalance, decimal futureInitialMargin)
    {
        return Math.Round((availableBalance - futureInitialMargin) / balance * 100, 2);
    }

    public decimal GetVolatility(decimal highPrice, decimal lowPrice)
    {
        if (highPrice == 0.0m || lowPrice == 0.0m)
        {
            return 0.0m;
        }

        return Math.Round((highPrice - lowPrice) * 100 / Math.Abs(lowPrice), 2);
    }
    
    public IEnumerable<int> GetIterationValues(int totalValue, int size)
    {
        var listOfIterations = new List<int>();
        while (true)
        {
            if (totalValue > size)
            {
                totalValue -= size;
                listOfIterations.Add(size);
            
                continue;
            }
        
            listOfIterations.Add(totalValue);
        
            break;
        }

        return listOfIterations;
    }
    
    public IEnumerable<double> GetIterationValues(double totalValue, double size)
    {
        var listOfIterations = new List<double>();
        while (true)
        {
            if (totalValue > size)
            {
                totalValue -= size;
                listOfIterations.Add(size);
            
                continue;
            }
        
            listOfIterations.Add(totalValue);
        
            break;
        }

        return listOfIterations;
    }

    public IEnumerable<decimal> SplitPositionQuantity(decimal quantity, decimal maxOrderQuantity)
    {
        if (quantity < maxOrderQuantity)
        {
            return new List<decimal> { quantity };
        }
        
        var orderSize = quantity;
        
        while (true)
        {
            orderSize -= orderSize / 2;

            if (orderSize < maxOrderQuantity)
            {
                break;
            }
        }

        return GetIterationValues(quantity, orderSize);
    }

    #region Private methods

    private static int GetCountDecimalsAfterPoint(decimal tickSize)
    {
        var valueString = tickSize.ToString(CultureInfo.InvariantCulture);
        if (!valueString.Contains('.'))
        {
            return 0;
        }

        var decimalsToLeaveAfterPoint = 0;

        var minOrderQuantityToString = tickSize.ToString(CultureInfo.InvariantCulture);
        var splitMinOrderQuantityValue = minOrderQuantityToString.Split('.');
        if (splitMinOrderQuantityValue[1].All(x => x == '0'))
        {
            return decimalsToLeaveAfterPoint;
        }

        for (var i = 0; i < splitMinOrderQuantityValue[1].Length; i++)
        {
            if (splitMinOrderQuantityValue[1][i] == '0')
            {
                continue;
            }

            decimalsToLeaveAfterPoint = i + 1;

            break;
        }

        return decimalsToLeaveAfterPoint;
    }
    
    private static IEnumerable<decimal> GetIterationValues(decimal totalValue, decimal size)
    {
        var listOfIterations = new List<decimal>();
        while (true)
        {
            if (totalValue > size)
            {
                totalValue -= size;
                listOfIterations.Add(size);
            
                continue;
            }
        
            listOfIterations.Add(totalValue);
        
            break;
        }

        return listOfIterations;
    }

    #endregion
}