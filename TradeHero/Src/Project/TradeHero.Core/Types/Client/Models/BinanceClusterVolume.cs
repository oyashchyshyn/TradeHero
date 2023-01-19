namespace TradeHero.Core.Types.Client.Models;

public class BinanceClusterVolume
{
    public decimal Price { get; set; }
    public decimal BuyVolume { get; set; }
    public decimal SellVolume { get; set; }
    public int SellTrades { get; set; }
    public int BuyTrades { get; set; }
}