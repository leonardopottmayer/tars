using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.FeatureFlags.Abstractions;
using Pottmayer.Tars.Security.Authorization.Abstractions;

namespace Pottmayer.Tars.Security.Authorization;

/// <summary>
/// The one check shared by the mediator behavior and the MVC filter: every permission's feature
/// flag must be on, then the subject must hold every permission.
/// </summary>
public sealed class AuthorizationGate(IPermissionCatalog catalog, IPermissionChecker checker, IServiceProvider services)
{
    /// <summary>
    /// <c>null</c> when allowed; otherwise a <see cref="ErrorType.NotFound"/> error
    /// (<see cref="FeatureFlagErrors.Disabled"/>) for a disabled feature, or a
    /// <see cref="ErrorType.Forbidden"/> error (<see cref="AuthorizationErrors.Denied"/>) for a missing
    /// permission or subject.
    /// </summary>
    /// <exception cref="InvalidOperationException">A permission was never declared.</exception>
    public async ValueTask<Error?> CheckAsync(
        string? subjectId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default)
    {
        foreach (var permission in permissions)
        {
            var definition = catalog.Find(permission) ?? throw new InvalidOperationException(
                $"Permission '{permission}' is not declared. Declare it with AddTarsPermissionDefinitions.");

            if (definition.Feature is { } feature
                && !await services.GetRequiredService<IFeatureFlags>().IsEnabledAsync(feature, cancellationToken))
                return Error.NotFound(FeatureFlagErrors.Disabled, $"Feature '{feature}' is disabled.");
        }

        if (subjectId is null)
            return Error.Forbidden(AuthorizationErrors.Denied, "The request has no subject to authorize.");

        foreach (var permission in permissions)
        {
            if (!await checker.HasAsync(subjectId, permission, cancellationToken))
                return Error.Forbidden(AuthorizationErrors.Denied, $"Permission '{permission}' is required.");
        }

        return null;
    }
}
