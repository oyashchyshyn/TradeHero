using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Repositories;
using TradeHero.Database.Context;
using TradeHero.Database.Entities;
using TradeHero.Database.Repositories;
using TradeHero.Database.Worker;

namespace TradeHero.Database;

public static class ThDatabaseServiceCollectionExtensions
{
    public static void AddThDatabase(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<DatabaseFileWorker>();
        
        serviceCollection.AddTransient(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>();
            var databaseFileWorker = serviceProvider.GetRequiredService<DatabaseFileWorker>();
            
            var context = new ThDatabaseContext(
                new DbContextOptions<ThDatabaseContext>(),
                logger.CreateLogger<ThDatabaseContext>(),
                databaseFileWorker
            );

            if (!context.Database.EnsureCreated())
            {
                return context;
            }
            
            var connections = databaseFileWorker.GetDataFromFile<Connection>();
            var strategies = databaseFileWorker.GetDataFromFile<Strategy>();
            var user = databaseFileWorker.GetDataFromFile<User>();

            context.Connections.AddRange(connections);
            context.Strategies.AddRange(strategies);
            context.User.AddRange(user);

            context.SaveChanges();
            
            return context;
        });

        serviceCollection.AddTransient<IUserRepository, UserRepository>();
        serviceCollection.AddTransient<IStrategyRepository, StrategyRepository>();
        serviceCollection.AddTransient<IConnectionRepository, ConnectionRepository>();
    }
}