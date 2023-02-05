namespace TradeHero.Core.Contracts.Client;

public interface IBinanceResolver
{
    IThRestBinanceClient? GenerateBinanceClient(string apiKey, string secretKey);
}