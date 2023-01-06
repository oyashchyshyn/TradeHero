namespace TradeHero.Contracts.Services.Models.Environment;

public class FolderSettings
{
    public string DataFolder { get; set; } = string.Empty;
    public string DatabaseFolder { get; set; } = string.Empty;
    public string LogsFolder { get; set; } = string.Empty;
    public string UpdateFolder { get; set; } = string.Empty;
}