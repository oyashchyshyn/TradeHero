using Microsoft.Extensions.DependencyInjection;
using TradeHero.Core.Types.Services;
using TradeHero.Services.Services;

namespace TradeHero.Services;

public static class ServicesDiContainer
{
    public static void AddServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IJsonService, JsonService>();
        serviceCollection.AddSingleton<ICalculatorService, CalculatorService>();
        serviceCollection.AddSingleton<IDateTimeService, DateTimeService>();
        serviceCollection.AddSingleton<IJobService, JobService>();
        serviceCollection.AddSingleton<IInternetConnectionService, InternetConnectionService>();
        serviceCollection.AddSingleton<IEnvironmentService, EnvironmentService>();
        serviceCollection.AddSingleton<IFileService, FileService>();
        serviceCollection.AddSingleton<IGithubService, GithubService>();
        serviceCollection.AddSingleton<ITerminalService, TerminalService>();
        serviceCollection.AddSingleton<IStoreService, StoreService>();
        serviceCollection.AddSingleton<IApplicationService, ApplicationService>();
    }
}