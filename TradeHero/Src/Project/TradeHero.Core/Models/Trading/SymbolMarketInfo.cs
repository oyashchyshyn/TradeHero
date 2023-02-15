using Binance.Net.Enums;
using Binance.Net.Objects.Models;
using Newtonsoft.Json;
using TradeHero.Core.Enums;

namespace TradeHero.Core.Models.Trading;

public class SymbolMarketInfo
{
    public string SpotName { get; init; } = string.Empty;
    public string FuturesUsdName { get; init; } = string.Empty;
    public string BaseFuturesUsdAsset { get; init; } = string.Empty;
    public string QuoteAsset { get; init; } = string.Empty;

    public KlinePower Power { get; set; }
    public KlineAction KlineAction { get; set; }
    public PositionSide PositionSide { get; set; }
    public bool IsPocInWick { get; set; }

    // Kline info
    public decimal KlineBuyVolume { get; set; }
    public decimal KlineSellVolume { get; set; }
    public decimal KlineDeltaVolume => KlineBuyVolume - KlineSellVolume;
    public decimal KlineVolumeCoefficient => GetCoefficient(KlineBuyVolume, KlineSellVolume);
    public decimal KlineTradedQuoteVolume { get; set; }
    
    // Poc volume info
    public decimal PocBuyVolume { get; set; }
    public decimal PocSellVolume { get; set; }
    public decimal PocDeltaVolume => PocBuyVolume - PocSellVolume;
    public decimal PocTradedQuoteVolume { get; set; }

    // Poc volume info
    public int PocBuyTrades { get; set; }
    public int PocSellTrades { get; set; }
    public int PocDeltaTrades => PocBuyTrades - PocSellTrades;
    public decimal PocTradesCoefficient => GetCoefficient(PocBuyTrades, PocSellTrades);
    
    // OrderBook info
    [JsonIgnore]
    public List<BinanceOrderBookEntry> Bids { get; } = new();
    [JsonIgnore]
    public List<BinanceOrderBookEntry> Asks { get; } = new();
    public decimal TotalBids => Bids.Sum(x => x.Quantity);
    public decimal TotalAsks => Asks.Sum(x => x.Quantity);
    public decimal AsksBidsCoefficient => GetCoefficient(TotalBids, TotalAsks);

    #region Private methods

    private static decimal GetCoefficient(decimal buys, decimal sells)
    {
        var delta = buys - sells;
        
        var result = delta switch
        {
            > 0 => buys / sells,
            < 0 => -(sells / buys),
            _ => 0
        };

        return Math.Round(result, 2);
    }

    #endregion
}