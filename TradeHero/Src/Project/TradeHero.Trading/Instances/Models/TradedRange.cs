namespace TradeHero.Trading.Instances.Models;

internal class TradedRange
{
    public int Index { get; init; }
    public decimal StartPrice { get; set; }
    public decimal EndPrice { get; set; }
    public decimal BuyVolume { get; set; }
    public decimal SellVolume { get; set; }
    public int SellTrades { get; set; }
    public int BuyTrades { get; set; }
}