namespace TradeHero.Contracts.Services.Models.Environment;

public class FolderSettings
{
    public string DataFolderName { get; set; } = string.Empty;
    public string DatabaseFolderName { get; set; } = string.Empty;
    public string LogsFolderName { get; set; } = string.Empty;
    public string UpdateFolderName { get; set; } = string.Empty;
}