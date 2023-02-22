using TradeHero.Core.Enums;

namespace TradeHero.Trading.Endpoints.Models;

public struct LastPriceResult
{
    public ActionResult ActionResult { get; }
    public decimal LastPrice { get; }

    public LastPriceResult(ActionResult actionResult, decimal lastPrice)
    {
        ActionResult = actionResult;
        LastPrice = lastPrice;
    }
}