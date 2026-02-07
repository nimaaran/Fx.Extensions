// ------------------------------------------------------------------------------------------------
// All rights reserved. This project and this file is published under the Apache License 2. So, Any
// use of this project must comply with the terms and policies described in the project’s `LICENSE`
// file. For more information, please visit the project repository on GitHub or contact with owner.
// ------------------------------------------------------------------------------------------------

using Fx.Common.Domains.Aggregates;
using Fx.Common.Domains.Entities;
using Fx.Common.Domains.Repositories;
using Fx.Common.Domains.Specifications;
using Fx.Common.Infrastructures.DataContexts;
using Fx.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Fx.Extensions.EF.DataContexts;

/// <summary>
/// A base class for EF-based data context providers.
/// </summary>
public abstract class BaseEFDataContext : DbContext, IDataContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEFDataContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    protected BaseEFDataContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<IDataModel> GetTrackedObjects()
    {
        var trackedObjects = this.ChangeTracker.Entries<IDataModel>();

        return trackedObjects.Select(r => r.Entity)
                             .ToList()
                             .AsReadOnly();
    }

    /// <inheritdoc/>
    public IQueryable<TModel> GetDataModel<TModel>()
        where TModel : class, IDataModel
        => this.Set<TModel>().AsQueryable();

    /// <inheritdoc/>
    public void AddRecord<TEntity>(TEntity instance)
        where TEntity : class, IEntity
        => this.Set<TEntity>().Entry(instance).State = EntityState.Added;

    /// <inheritdoc/>
    public void DeleteRecord<TEntity>(TEntity instance)
        where TEntity : class, IEntity
        => this.Set<TEntity>().Entry(instance).State = EntityState.Deleted;

    /// <inheritdoc/>
    public void UpdateRecord<TEntity>(TEntity instance)
        where TEntity : class, IEntity
        => this.Set<TEntity>().Entry(instance).State = EntityState.Modified;

    /// <inheritdoc/>
    public Task<List<TModel>> GetRecordsAsync<TModel>(
        int pageSize,
        int pageIndex,
        Sorter<TModel> sorter,
        ISpecification<TModel> specification,
        CancellationToken token)
        where TModel : class, IDataModel
    {
        var source = this.GetDataModel<TModel>();
        var builder = new QueryBuilder();
        var query = builder.Build(source, pageSize, pageIndex, sorter, specification);

        return query.ToListAsync(token);
    }

    /// <inheritdoc/>
    public new Task<int> SaveChangesAsync(CancellationToken token = default)
        => base.SaveChangesAsync(token);

    /// <summary>
    /// Define an entity key (PK) in the data context model.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity class.</typeparam>
    /// <typeparam name="TId">Type of the entity's primary key.</typeparam>
    /// <param name="modelBuilder">The EF model builder.</param>
    protected static void DefineKey<TEntity, TId>(ModelBuilder modelBuilder)
        where TEntity : class, IEntity<TId>
        where TId : notnull
        => modelBuilder.Entity<TEntity>().HasKey("Id");

    /// <summary>
    /// Defines an aggregate lock in the data context model.
    /// </summary>
    /// <typeparam name="TAggregateRoot">Type of the aggregate root.</typeparam>
    /// <param name="modelBuilder">The EF model builder.</param>
    protected static void DefineAggregateLock<TAggregateRoot>(ModelBuilder modelBuilder)
        where TAggregateRoot : class, IAggregateRoot
        => modelBuilder.Entity<TAggregateRoot>()
                       .OwnsOne(e => e.Lock, lockBuilder =>
                       {
                           lockBuilder.Property(l => l.Version).HasColumnName("Version");
                           lockBuilder.Property(l => l.Timestamp).HasColumnName("Timestamp");
                       });
}
