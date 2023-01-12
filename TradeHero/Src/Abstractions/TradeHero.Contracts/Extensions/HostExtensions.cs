using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TradeHero.Core.Constants;

namespace TradeHero.Contracts.Extensions;

public static class HostExtensions
{
    public static IHostBuilder UseRunningType(this IHostBuilder hostBuilder, string runningType)
    {
        return hostBuilder.ConfigureHostConfiguration(configBuilder =>
        {
            configBuilder.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>(HostConstants.RunningType, runningType)
            });
        });
    }
}