using Microsoft.Extensions.Configuration;
using Pottmayer.Tars.Data.Document.Abstractions.Connection;

namespace Pottmayer.Tars.Data.Document.MongoDB.Connection;

/// <summary>
/// Resolves MongoDB connections from <c>appsettings.json</c>, mirroring the relational resolver's precedence:
/// <list type="bullet">
///   <item><c>Tars:Data:Mongo:TenantConnections:{key}:{tenantKey}</c> — per-tenant connection (checked first).</item>
///   <item><c>Tars:Data:Mongo:TenantConnectionTemplates:{key}</c> — template with <c>{tenantKey}</c> / <c>{tenantCode}</c> placeholders.</item>
///   <item><c>Tars:Data:Mongo:Connections:{key}</c> — static/shared connection.</item>
/// </list>
/// Each entry provides a <c>ConnectionString</c> and a <c>Database</c> name.
/// </summary>
public sealed class ConfigurationMongoConnectionResolver : IMongoConnectionResolver
{
    private readonly IConfiguration _configuration;

    /// <summary>Creates the resolver over the application configuration.</summary>
    /// <param name="configuration">The configuration to read connections from.</param>
    public ConfigurationMongoConnectionResolver(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc/>
    public Task<IMongoConnectionDescriptor?> ResolveAsync(
        MongoConnectionResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var key = context.DatabaseKey;

        if (context.TenantKey is not null)
        {
            var tenantSection = _configuration.GetSection($"Tars:Data:Mongo:TenantConnections:{key}:{context.TenantKey}");
            var tenantCs = tenantSection["ConnectionString"];
            if (!string.IsNullOrWhiteSpace(tenantCs))
                return Task.FromResult<IMongoConnectionDescriptor?>(Build(key, tenantCs, tenantSection, context, context.TenantKey));

            var tmplSection = _configuration.GetSection($"Tars:Data:Mongo:TenantConnectionTemplates:{key}");
            var template = tmplSection["Template"];
            if (!string.IsNullOrWhiteSpace(template))
            {
                var cs = Expand(template, context);
                return Task.FromResult<IMongoConnectionDescriptor?>(Build(key, cs, tmplSection, context, context.TenantKey, isTenantScoped: true));
            }
        }

        var staticSection = _configuration.GetSection($"Tars:Data:Mongo:Connections:{key}");
        var staticCs = staticSection["ConnectionString"];
        if (!string.IsNullOrWhiteSpace(staticCs))
            return Task.FromResult<IMongoConnectionDescriptor?>(Build(key, staticCs, staticSection, context));

        return Task.FromResult<IMongoConnectionDescriptor?>(null);
    }

    private static string Expand(string value, MongoConnectionResolutionContext context)
        => value
            .Replace("{tenantKey}", context.TenantKey)
            .Replace("{tenantCode}", context.TenantCode ?? context.TenantKey);

    private static MongoConnectionDescriptor Build(
        string key, string cs, IConfiguration section, MongoConnectionResolutionContext context,
        string? tenantKey = null, bool isTenantScoped = false)
    {
        var databaseRaw = section["Database"]
            ?? throw new InvalidOperationException(
                $"Mongo connection for key '{key}' is missing the required 'Database' name.");
        var database = tenantKey is null ? databaseRaw : Expand(databaseRaw, context);
        return new MongoConnectionDescriptor
        {
            DatabaseKey = key,
            ConnectionString = cs,
            DatabaseName = database,
            IsTenantScoped = isTenantScoped || tenantKey is not null,
            TenantKey = tenantKey
        };
    }
}
