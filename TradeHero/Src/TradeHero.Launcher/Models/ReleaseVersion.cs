namespace TradeHero.Launcher.Models;

internal class ReleaseVersion
{
    public bool IsNewAvailable { get; init; }
    public string DownloadUri { get; init; } = string.Empty;
}