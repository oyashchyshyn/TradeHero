namespace TradeHero.Contracts.Services.Models.Update;

public class DownloadResponse
{
    public string AppFileName { get; init; } = string.Empty;
    public string AppFileLocation { get; init; } = string.Empty;
    public string UpdaterFileName { get; init; } = string.Empty;
    public string UpdaterFileLocation { get; init; } = string.Empty;
}