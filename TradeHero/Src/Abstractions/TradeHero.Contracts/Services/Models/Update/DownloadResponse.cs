namespace TradeHero.Contracts.Services.Models.Update;

public class DownloadResponse
{
    public string UpdateFolderPath { get; init; }
    public string AppFileName { get; init; }
    public string UpdaterFolderPath { get; init; }
    public string UpdaterFileName { get; init; }
}