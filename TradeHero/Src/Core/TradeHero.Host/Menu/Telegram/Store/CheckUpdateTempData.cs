using TradeHero.Contracts.Services.Models.Update;

namespace TradeHero.EntryPoint.Menu.Telegram.Store;

internal class CheckUpdateTempData
{
    public ReleaseVersion? ReleaseVersion { get; set; }

    public void ClearData()
    {
        ReleaseVersion = null;
    }
}