using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Options;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.DI;

public static class OutboxOptionsDI
{
    /// <summary>
    /// Binds <see cref="OutboxOptions"/> from configuration (default section
    /// <c>Tars:Messaging:Outbox</c>) as the fleet-wide relay defaults.
    /// </summary>
    /// <remarks>
    /// Only the operational tuning comes from configuration — polling, batch, retry budget, lease and
    /// retention differ per environment. The backoff function and any per-database override stay in code
    /// (via <c>AddTarsOutboxRelay</c>'s <c>configure</c>), the same way the broker options keep
    /// connection settings in configuration but subscriptions in code. Optional: without this call each
    /// relay simply uses the built-in defaults.
    /// </remarks>
    public static OptionsBuilder<OutboxOptions> AddTarsOutboxOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<OutboxOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        sectionName ??= OutboxOptions.SectionName;

        var ob = builder.Services
            .AddOptions<OutboxOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .Validate(OutboxOptionsValidation.Validate, OutboxOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
