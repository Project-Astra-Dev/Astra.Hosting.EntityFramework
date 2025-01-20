using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace Astra.Hosting.EntityFramework.Contexts;

public interface IAstraDatabaseContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    string DatabaseName { get; }
}

public abstract class AstraDatabaseContextBase : DbContext, IAstraDatabaseContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source=astra_{DatabaseName.ToLower()}.db");

    public abstract string DatabaseName { get; }
}