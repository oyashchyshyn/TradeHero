using TradeHero.Core.Types.Services.Models.Github;

namespace TradeHero.Application.Menu.Telegram.Store;

internal class CheckUpdateTempData
{
    public ReleaseVersion? ReleaseVersion { get; set; }

    public void ClearData()
    {
        ReleaseVersion = null;
    }
}