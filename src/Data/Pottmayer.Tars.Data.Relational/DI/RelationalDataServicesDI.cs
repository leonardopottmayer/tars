using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Keys;
using Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;
using Pottmayer.Tars.Data.Relational.DataConnection;
using Pottmayer.Tars.Data.Relational.DataContext;

namespace Pottmayer.Tars.Data.Relational.DI;

/// <summary>
/// Registration for the relational data axis (EF Core + Dapper). Provider-agnostic infrastructure
/// (context accessor, context factory, unit-of-work factory, multi-database coordination, repository
/// scanning) lives in <c>Pottmayer.Tars.Data.DI.DataServicesDI</c> and must be registered separately.
/// </summary>
public static class RelationalDataServicesDI
{
    // ─────────────────────────────────────────────────────────────────────────
    // Connection resolution (relational-specific)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers <see cref="CompositeDataConnectionResolver"/> as the main <see cref="IDataConnectionResolver"/> Singleton.
    /// The composite chains all resolvers registered via <see cref="AddTarsRelationalConfigurationConnectionResolver"/>
    /// or custom <c>TryAddEnumerable</c> calls, returning the first non-null result.
    /// </summary>
    public static IServiceCollection AddTarsRelationalCompositeConnectionResolver(this IServiceCollection services)
    {
        services.TryAddSingleton<IDataConnectionResolver, CompositeDataConnectionResolver>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="ConfigurationDataConnectionResolver"/> to the <see cref="IDataConnectionResolver"/> enumerable.
    /// Reads connection strings from <c>Tars:Data:Connections</c> in <c>appsettings.json</c>.
    /// Call <see cref="AddTarsRelationalCompositeConnectionResolver"/> to wire the chain.
    /// </summary>
    public static IServiceCollection AddTarsRelationalConfigurationConnectionResolver(this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IDataConnectionResolver, ConfigurationDataConnectionResolver>());
        return services;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AddTarsRelationalData — relational database pipeline registration
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers the relational data pipeline for a <b>single-database</b> application
    /// using the <c>"default"</c> key.
    /// </summary>
    /// <remarks>
    /// Provider-agnostic infrastructure must be registered separately (see
    /// <c>AddTarsDataContextAccessor</c>, <c>AddTarsDataContextFactory</c>, <c>AddTarsUnitOfWorkFactory</c>),
    /// plus <see cref="AddTarsRelationalCompositeConnectionResolver"/> and
    /// <see cref="AddTarsRelationalConfigurationConnectionResolver"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddTarsRelationalData&lt;AppDbContext&gt;((sp, d) =>
    ///     new DbContextOptionsBuilder&lt;AppDbContext&gt;()
    ///         .UseNpgsql(d.ConnectionString)
    ///         .Options);
    /// </code>
    /// </example>
    public static IServiceCollection AddTarsRelationalData<TDbContext>(
        this IServiceCollection services,
        Func<IServiceProvider, IDataConnectionDescriptor, DbContextOptions<TDbContext>> buildOptions)
        where TDbContext : RelationalDbContext
        => services.AddTarsRelationalData(DataKeys.Default, buildOptions);

    /// <summary>
    /// Registers the relational data pipeline for a named database key.
    /// Call once per relational database in multi-database applications.
    /// </summary>
    /// <remarks>
    /// Provider-agnostic infrastructure must be registered separately.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddTarsRelationalData&lt;AppDbContext&gt;("default", (sp, d) =>
    ///     new DbContextOptionsBuilder&lt;AppDbContext&gt;().UseNpgsql(d.ConnectionString).Options);
    ///
    /// services.AddTarsRelationalData&lt;CentralDbContext&gt;("central", (sp, d) =>
    ///     new DbContextOptionsBuilder&lt;CentralDbContext&gt;().UseNpgsql(d.ConnectionString).Options);
    /// </code>
    /// </example>
    public static IServiceCollection AddTarsRelationalData<TDbContext>(
        this IServiceCollection services,
        string databaseKey,
        Func<IServiceProvider, IDataConnectionDescriptor, DbContextOptions<TDbContext>> buildOptions)
        where TDbContext : RelationalDbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseKey);
        ArgumentNullException.ThrowIfNull(buildOptions);

        services.AddScoped<IKeyedDataContextFactory>(sp =>
            new RelationalDataContextFactory<TDbContext>(
                databaseKey,
                sp.GetRequiredService<IDataConnectionResolver>(),
                buildOptions,
                sp,
                sp.GetRequiredService<IDataContextAccessor>(),
                sp.GetService<IDomainEventDispatcher>()));

        return services;
    }
}
