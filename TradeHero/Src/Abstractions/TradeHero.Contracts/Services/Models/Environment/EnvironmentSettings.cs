namespace TradeHero.Contracts.Services.Models.Environment;

public class EnvironmentSettings
{
    public ClientSettings Client { get; set; } = new();
    public InternetConnectionSettings InternetConnection { get; set; } = new();
    public LoggerSettings Logger { get; set; } = new();
    public GithubSettings Github { get; set; } = new();
}