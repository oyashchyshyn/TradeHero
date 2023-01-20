namespace TradeHero.Core.Types.Client.Resolvers;

public interface IBinanceResolver
{
    IThRestBinanceClient? GenerateBinanceClient(string apiKey, string secretKey);
}