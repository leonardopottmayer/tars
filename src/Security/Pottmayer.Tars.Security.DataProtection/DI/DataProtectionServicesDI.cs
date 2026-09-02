using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Security.DataProtection.Abstractions;

namespace Pottmayer.Tars.Security.DataProtection.DI;

public static class DataProtectionServicesDI
{
    /// <summary>
    /// Registers <see cref="AesGcmSecretProtector"/> as the <see cref="ISecretProtector"/>. Pair with
    /// <see cref="DataProtectionOptionsDI.AddTarsDataProtectionOptions"/> to supply the keys.
    /// </summary>
    /// <remarks>
    /// Registered as a singleton: the keys are read and validated once at construction, and the
    /// protector holds no per-request state.
    /// </remarks>
    public static IServiceCollection AddTarsSecretProtector(this IServiceCollection services)
    {
        services.TryAddSingleton<ISecretProtector, AesGcmSecretProtector>();
        return services;
    }
}
