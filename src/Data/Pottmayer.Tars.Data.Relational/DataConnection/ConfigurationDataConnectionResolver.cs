using Microsoft.Extensions.Configuration;
using Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;
using Pottmayer.Tars.Data.Relational.Abstractions.Enums;

namespace Pottmayer.Tars.Data.Relational.DataConnection;

/// <summary>
/// Resolves connections from <c>appsettings.json</c>.
/// <list type="bullet">
///   <item><c>Tars:Data:Connections:{key}</c> — global connections.</item>
///   <item><c>Tars:Data:TenantConnections:{key}:{tenantKey}</c> — per-tenant connections (checked first).</item>
///   <item><c>Tars:Data:TenantConnectionTemplates:{key}:Template</c> — template with <c>{tenantKey}</c> / <c>{tenantCode}</c> placeholders.</item>
/// </list>
/// </summary>
public sealed class ConfigurationDataConnectionResolver : IDataConnectionResolver
{
    private readonly IConfiguration _configuration;

    public ConfigurationDataConnectionResolver(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public Task<IDataConnectionDescriptor?> ResolveAsync(
        DataConnectionResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var key = context.DatabaseKey;

        // 1. Per-tenant explicit connection
        if (context.TenantKey is not null)
        {
            var tenantSection = _configuration.GetSection($"Tars:Data:TenantConnections:{key}:{context.TenantKey}");
            var tenantCs = tenantSection["ConnectionString"];
            if (!string.IsNullOrWhiteSpace(tenantCs))
                return Task.FromResult<IDataConnectionDescriptor?>(Build(key, tenantCs, tenantSection, context.TenantKey));

            // 2. Template substitution
            var tmplSection = _configuration.GetSection($"Tars:Data:TenantConnectionTemplates:{key}");
            var template = tmplSection["Template"];
            if (!string.IsNullOrWhiteSpace(template))
            {
                var cs = template
                    .Replace("{tenantKey}", context.TenantKey)
                    .Replace("{tenantCode}", context.TenantCode ?? context.TenantKey);
                return Task.FromResult<IDataConnectionDescriptor?>(Build(key, cs, tmplSection, context.TenantKey, isTenantScoped: true));
            }
        }

        // 3. Static global connection
        var staticSection = _configuration.GetSection($"Tars:Data:Connections:{key}");
        var staticCs = staticSection["ConnectionString"];
        if (!string.IsNullOrWhiteSpace(staticCs))
            return Task.FromResult<IDataConnectionDescriptor?>(Build(key, staticCs, staticSection));

        return Task.FromResult<IDataConnectionDescriptor?>(null);
    }

    private static DataConnectionDescriptor Build(
        string key, string cs, IConfiguration section,
        string? tenantKey = null, bool isTenantScoped = false)
    {
        var providerStr = section["Provider"] ?? string.Empty;
        var provider = Enum.TryParse<DbProvider>(providerStr, ignoreCase: true, out var p) ? p : DbProvider.Unknown;
        return new DataConnectionDescriptor
        {
            DatabaseKey = key,
            ConnectionString = cs,
            Provider = provider,
            IsTenantScoped = isTenantScoped || tenantKey is not null,
            TenantKey = tenantKey
        };
    }
}
