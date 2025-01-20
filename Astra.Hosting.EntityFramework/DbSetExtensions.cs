using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Astra.Hosting.EntityFramework;

public static class DbSetExtensions
{
    /// <summary>
    /// Adds an entity if it doesn't exist based on a predicate
    /// </summary>
    public static async Task AddPreservedAsync<TEntity>(
            this DbSet<TEntity> dbSet, 
            Expression<Func<TEntity, bool>> predicate,
            TEntity entity,
            CancellationToken cancellationToken = default
        ) where TEntity : class
    {
        if (!await dbSet.AnyAsync(predicate, cancellationToken))
            await dbSet.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Updates an entity if it exists, adds it if it doesn't
    /// </summary>
    public static async Task UpsertAsync<TEntity>(
            this DbSet<TEntity> dbSet,
            Expression<Func<TEntity, bool>> predicate,
            TEntity entity,
            Action<TEntity, TEntity> updateAction,
            CancellationToken cancellationToken = default
        ) where TEntity : class
    {
        var existing = await dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
        if (existing != null) 
            updateAction(existing, entity);
        else
        {
            await dbSet.AddAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// Adds a range of entities, skipping any that already exist based on a key selector
    /// </summary>
    public static async Task AddRangePreservedAsync<TEntity, TKey>(
            this DbSet<TEntity> dbSet,
            IEnumerable<TEntity> entities,
            Func<TEntity, TKey> keySelector,
            CancellationToken cancellationToken = default
        ) where TEntity : class
    {
        var existingKeys = dbSet.Select(keySelector).ToHashSet();
        var newEntities = entities.Where(e => !existingKeys.Contains(keySelector(e)));
        await dbSet.AddRangeAsync(newEntities, cancellationToken);
    }

    /// <summary>
    /// Soft deletes an entity by setting a flag rather than removing it from the database
    /// </summary>
    public static async Task SoftDeleteAsync<TEntity>(
            this DbSet<TEntity> dbSet,
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, bool>> isDeletedProperty,
            CancellationToken cancellationToken = default
        ) where TEntity : class
    {
        var entity = await dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
        if (entity != null)
        {
            var parameter = isDeletedProperty.Parameters.Single();
            var setter = Expression.Lambda<Action<TEntity>>(
                Expression.Assign(isDeletedProperty.Body, Expression.Constant(true)),
                parameter
            );
            setter.Compile()(entity);
        }
    }

    /// <summary>
    /// Gets an entity by predicate or creates a new one using the factory function
    /// </summary>
    public static async Task<TEntity> GetOrCreateAsync<TEntity>(
            this DbSet<TEntity> dbSet,
            Expression<Func<TEntity, bool>> predicate,
            Func<Task<TEntity>> factory,
            CancellationToken cancellationToken = default
        ) where TEntity : class
    {
        var entity = await dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
        if (entity == null)
        {
            entity = await factory();
            await dbSet.AddAsync(entity, cancellationToken);
        }
        return entity;
    }

    /// <summary>
    /// Retrieves a page of entities with total count
    /// </summary>
    public static async Task<(List<TEntity> Items, int TotalCount)> GetPagedAsync<TEntity>(
            this DbSet<TEntity> dbSet,
            Expression<Func<TEntity, bool>> predicate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default
        ) where TEntity : class
    {
        var query = dbSet.Where(predicate);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}