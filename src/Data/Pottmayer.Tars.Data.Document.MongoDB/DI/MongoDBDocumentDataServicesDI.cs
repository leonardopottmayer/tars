using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Keys;
using Pottmayer.Tars.Data.Document.Abstractions.Connection;
using Pottmayer.Tars.Data.Document.MongoDB.Connection;
using Pottmayer.Tars.Data.Document.MongoDB.DataContext;

namespace Pottmayer.Tars.Data.Document.MongoDB.DI;

/// <summary>
/// Registration for the MongoDB document data axis. Provider-agnostic infrastructure (context accessor,
/// context factory, unit-of-work factory, multi-database coordination, repository scanning) lives in
/// <c>Pottmayer.Tars.Data.DI.DataServicesDI</c> and must be registered separately — the same infrastructure
/// drives relational and document keys side by side.
/// </summary>
public static class MongoDBDocumentDataServicesDI
{
    // ─────────────────────────────────────────────────────────────────────────
    // Connection resolution (Mongo-specific)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers <see cref="CompositeMongoConnectionResolver"/> as the main <see cref="IMongoConnectionResolver"/> Singleton.
    /// The composite chains all resolvers registered via <see cref="AddTarsMongoConfigurationConnectionResolver"/>
    /// or custom <c>TryAddEnumerable</c> calls, returning the first non-null result.
    /// </summary>
    public static IServiceCollection AddTarsMongoCompositeConnectionResolver(this IServiceCollection services)
    {
        services.TryAddSingleton<IMongoConnectionResolver, CompositeMongoConnectionResolver>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="ConfigurationMongoConnectionResolver"/> to the <see cref="IMongoConnectionResolver"/> enumerable.
    /// Reads connection strings from <c>Tars:Data:Mongo:Connections</c> in <c>appsettings.json</c>.
    /// Call <see cref="AddTarsMongoCompositeConnectionResolver"/> to wire the chain.
    /// </summary>
    public static IServiceCollection AddTarsMongoConfigurationConnectionResolver(this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IMongoConnectionResolver, ConfigurationMongoConnectionResolver>());
        return services;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AddTarsMongoData — document database pipeline registration
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers the MongoDB data pipeline for a <b>single-database</b> application using the <c>"default"</c> key.
    /// </summary>
    /// <remarks>
    /// Provider-agnostic infrastructure must be registered separately (see <c>AddTarsDataContextAccessor</c>,
    /// <c>AddTarsDataContextFactory</c>, <c>AddTarsUnitOfWorkFactory</c>), plus
    /// <see cref="AddTarsMongoCompositeConnectionResolver"/> and <see cref="AddTarsMongoConfigurationConnectionResolver"/>.
    /// </remarks>
    public static IServiceCollection AddTarsMongoData(this IServiceCollection services)
        => services.AddTarsMongoData(DataKeys.Default);

    /// <summary>
    /// Registers the MongoDB data pipeline for a named database key.
    /// Call once per document database in multi-database applications.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddTarsMongoData("catalog");
    /// services.AddTarsMongoData("events");
    /// </code>
    /// </example>
    public static IServiceCollection AddTarsMongoData(this IServiceCollection services, string databaseKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseKey);

        services.AddScoped<IKeyedDataContextFactory>(sp =>
            new MongoDataContextFactory(
                databaseKey,
                sp.GetRequiredService<IMongoConnectionResolver>(),
                sp,
                sp.GetRequiredService<IDataContextAccessor>(),
                sp.GetService<IDomainEventDispatcher>()));

        return services;
    }
}
