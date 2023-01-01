using TradeHero.Contracts.Settings.Models;

namespace TradeHero.Contracts.Settings;

public class AppSettings
{
    public ClientSettings Client { get; set; } = new();
    public InternetConnectionSettings InternetConnection { get; set; } = new();
    public LoggerSettings Logger { get; set; } = new();
    public GithubSettings Github { get; set; } = new();
}