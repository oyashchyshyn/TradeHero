using Binance.Net.Clients;
using Binance.Net.Objects;
using TradeHero.Core.Contracts.Client;

namespace TradeHero.Client.Clients;

internal class ThSocketBinanceClient : BinanceSocketClient, IThSocketBinanceClient
{
    public ThSocketBinanceClient(BinanceSocketClientOptions options) 
        : base(options)
    { }
}