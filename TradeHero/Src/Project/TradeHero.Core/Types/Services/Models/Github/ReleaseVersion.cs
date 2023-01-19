namespace TradeHero.Core.Types.Services.Models.Github;

public class ReleaseVersion
{
    public bool IsNewAvailable { get; init; }
    public Version Version { get; init; } = new();
    public string AppName { get; init; } = string.Empty;
    public string AppDownloadUri { get; init; } = string.Empty;
    public string LauncherName { get; init; } = string.Empty;
    public string LauncherDownloadUri { get; init; } = string.Empty;
}