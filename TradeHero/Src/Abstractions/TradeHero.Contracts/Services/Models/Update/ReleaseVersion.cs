namespace TradeHero.Contracts.Services.Models.Update;

public class ReleaseVersion
{
    public bool IsNewAvailable { get; init; }
    public Version Version { get; init; } = new();
    public string AppName { get; init; } = string.Empty;
    public string AppDownloadUri { get; init; } = string.Empty;
}