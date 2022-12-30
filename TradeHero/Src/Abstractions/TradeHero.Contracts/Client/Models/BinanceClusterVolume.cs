namespace TradeHero.Contracts.Client.Models;

public class BinanceClusterVolume
{
    public int Index { get; init; }
    public decimal StartPrice { get; set; }
    public decimal EndPrice { get; set; }
    public decimal BuyVolume { get; set; }
    public decimal SellVolume { get; set; }
    public int SellOrders { get; set; }
    public int BuyOrders { get; set; }
    public int TotalOrders => BuyOrders + SellOrders;
    public int DeltaOrders => BuyOrders - SellOrders;
    public decimal TotalVolume => SellVolume + BuyVolume;
    public decimal Delta => BuyVolume - SellVolume;
}