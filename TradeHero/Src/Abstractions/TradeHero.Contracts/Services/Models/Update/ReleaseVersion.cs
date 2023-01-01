namespace TradeHero.Contracts.Services.Models.Update;

public class ReleaseVersion
{
    public bool IsNewAvailable { get; init; }
    public Version Version { get; init; }
    public string DownloadUri { get; init; }
}