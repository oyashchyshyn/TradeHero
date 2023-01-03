namespace TradeHero.Contracts.Client.Resolvers;

public interface IBinanceResolver
{
    IThRestBinanceClient? GenerateBinanceClient(string apiKey, string secretKey);
}