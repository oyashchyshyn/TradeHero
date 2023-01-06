namespace TradeHero.Contracts.Services.Models.Environment;

public class ApplicationSettings
{
    public string BaseAppName { get; set; } = string.Empty;
    public string WindowsAppName { get; set; } = string.Empty;
    public string LinuxAppName { get; set; } = string.Empty;
}