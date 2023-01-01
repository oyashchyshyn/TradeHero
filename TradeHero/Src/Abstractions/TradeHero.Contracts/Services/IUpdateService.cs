using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.Services.Models.Update;

namespace TradeHero.Contracts.Services;

public interface IUpdateService
{
    Task<GenericBaseResult<ReleaseVersion>> IsNewVersionAvailableAsync();
    Task UpdateApplicationAsync(ReleaseVersion releaseVersion);
}