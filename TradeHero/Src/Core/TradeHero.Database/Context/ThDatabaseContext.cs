using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Database.Entities;
using TradeHero.Database.Worker;

namespace TradeHero.Database.Context;

internal class ThDatabaseContext : DbContext
{
    private readonly ILogger<ThDatabaseContext> _logger;
    private readonly DatabaseFileWorker _databaseFileWorker;

    public DbSet<User> User { get; set; } = null!;
    public DbSet<Strategy> Strategies { get; set; } = null!;
    public DbSet<Connection> Connections { get; set; } = null!;

    public ThDatabaseContext(
        DbContextOptions<ThDatabaseContext> options, 
        ILogger<ThDatabaseContext> logger, 
        DatabaseFileWorker databaseFileWorker
        ) 
        : base(options)
    {
        _logger = logger;
        _databaseFileWorker = databaseFileWorker;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: DatabaseConstants.DatabaseName);
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var modifiedEntitiesName = GetModifiedEntitiesName();
        
            var result = await base.SaveChangesAsync(cancellationToken);
            if (result > 0)
            {
                UpdateFiles(modifiedEntitiesName);
            }
        
            return result;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SaveChangesAsync));
            
            return -1;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SaveChangesAsync));
            
            return -1;
        }
    }

    public override int SaveChanges()
    {
        try
        {
            var modifiedEntitiesName = GetModifiedEntitiesName();
            
            var result = base.SaveChanges();
            if (result > 0)
            {
                UpdateFiles(modifiedEntitiesName);
            }
                
            return result;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SaveChanges));
            
            return -1;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SaveChanges));
                
            return -1;
        }
    }

    #region Private methods

    private void UpdateFiles(IEnumerable<string> entitiesName)
    {
        foreach (var modifiedEntity in entitiesName)
        {
            switch (modifiedEntity)
            {
                case nameof(Connection):
                    _databaseFileWorker.UpdateDataInFile(
                        Connections.AsNoTracking().ToList()
                    );
                    break;
                case nameof(Strategy):
                    _databaseFileWorker.UpdateDataInFile(
                        Strategies.AsNoTracking().ToList()
                    );
                    break;
                case nameof(Entities.User):
                    _databaseFileWorker.UpdateDataInFile(
                        User.AsNoTracking().ToList()
                    );
                    break;
            }
        }
    }

    private IEnumerable<string> GetModifiedEntitiesName()
    {
        return ChangeTracker.Entries()
            .Where(p => p.State == EntityState.Modified || p.State == EntityState.Added || p.State == EntityState.Deleted)
            .Select(x => x.Entity.GetType().Name)
            .Distinct()
            .ToList();
    }

    #endregion
}