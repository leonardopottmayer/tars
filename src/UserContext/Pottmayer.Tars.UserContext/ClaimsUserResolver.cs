using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.UserContext.Abstractions;
using Pottmayer.Tars.UserContext.Options;

namespace Pottmayer.Tars.UserContext;

/// <summary>
/// Resolves a typed user instance from claims by mapping claim values to writable public properties.
/// Property metadata is cached per type for performance.
/// </summary>
/// <typeparam name="TUser">The user type (must have a parameterless constructor or be instantiable via Activator).</typeparam>
public sealed class ClaimsUserResolver<TUser> : IUserResolver<TUser>
    where TUser : class
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<PropertyMapping>> PropertyCache = new();

    private readonly IOptionsMonitor<UserContextOptions> _options;

    /// <summary>
    /// Creates a new resolver.
    /// </summary>
    public ClaimsUserResolver(IOptionsMonitor<UserContextOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public TUser Resolve(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var options = _options.CurrentValue;
        var user = Activator.CreateInstance<TUser>();
        var mappings = GetOrCreateMappings(typeof(TUser));

        foreach (var mapping in mappings)
        {
            var claimValue = GetClaimValue(principal, mapping);
            if (claimValue is null)
                continue;

            if (!TryConvert(claimValue, mapping.Property.PropertyType, options.ThrowOnConversionError, out var converted))
            {
                if (options.ThrowOnConversionError)
                    throw new InvalidOperationException(
                        $"Failed to convert claim value '{claimValue}' to type {mapping.Property.PropertyType} for property {mapping.Property.DeclaringType?.Name}.{mapping.Property.Name}.");
                continue;
            }

            mapping.Property.SetValue(user, converted);
        }

        return user;
    }

    private static string? GetClaimValue(ClaimsPrincipal principal, PropertyMapping mapping)
    {
        foreach (var claimType in mapping.ClaimTypes)
        {
            var claim = principal.FindFirst(c => string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase));
            if (claim is not null && !string.IsNullOrEmpty(claim.Value))
                return claim.Value;
        }
        return null;
    }

    private static IReadOnlyList<PropertyMapping> GetOrCreateMappings(Type userType)
    {
        return PropertyCache.GetOrAdd(userType, static type =>
        {
            var list = new List<PropertyMapping>();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var setter = prop.GetSetMethod();
                if (setter is null || !setter.IsPublic)
                    continue;

                var claimAttr = prop.GetCustomAttribute<ClaimAttribute>();
                var claimTypes = claimAttr is not null
                    ? new[] { claimAttr.Name }
                    : GetClaimTypesForProperty(prop.Name);
                list.Add(new PropertyMapping(prop, claimTypes));
            }
            return list;
        });
    }

    private static string[] GetClaimTypesForProperty(string propertyName)
    {
        return propertyName switch
        {
            "Id" => new[] { ClaimTypes.NameIdentifier, "sub", "Id" },
            "Name" => new[] { ClaimTypes.Name, "name", "Name" },
            "Email" => new[] { ClaimTypes.Email, "email", "Email" },
            _ => new[] { propertyName }
        };
    }

    private static bool TryConvert(string value, Type targetType, bool throwOnError, out object? result)
    {
        result = null;
        if (string.IsNullOrEmpty(value))
        {
            if (!targetType.IsValueType || IsNullable(targetType))
            {
                result = null;
                return true;
            }
            return !throwOnError;
        }

        var effectiveType = UnwrapNullable(targetType);

        try
        {
            if (effectiveType == typeof(string))
            {
                result = value;
                return true;
            }
            if (effectiveType == typeof(Guid))
            {
                result = Guid.Parse(value);
                return true;
            }
            if (effectiveType == typeof(int))
            {
                result = int.Parse(value, CultureInfo.InvariantCulture);
                return true;
            }
            if (effectiveType == typeof(long))
            {
                result = long.Parse(value, CultureInfo.InvariantCulture);
                return true;
            }
            if (effectiveType == typeof(short))
            {
                result = short.Parse(value, CultureInfo.InvariantCulture);
                return true;
            }
            if (effectiveType == typeof(byte))
            {
                result = byte.Parse(value, CultureInfo.InvariantCulture);
                return true;
            }
            if (effectiveType == typeof(bool))
            {
                result = bool.Parse(value);
                return true;
            }
            if (effectiveType == typeof(double))
            {
                result = double.Parse(value, CultureInfo.InvariantCulture);
                return true;
            }
            if (effectiveType == typeof(decimal))
            {
                result = decimal.Parse(value, CultureInfo.InvariantCulture);
                return true;
            }
            if (effectiveType == typeof(DateTime))
            {
                result = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                return true;
            }
            if (effectiveType == typeof(DateTimeOffset))
            {
                result = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                return true;
            }
            if (effectiveType.IsEnum)
            {
                result = Enum.Parse(effectiveType, value, ignoreCase: true);
                return true;
            }
        }
        catch
        {
            if (throwOnError)
                throw;
            return false;
        }

        if (throwOnError)
            throw new NotSupportedException($"No conversion from string to {effectiveType}.");
        return false;
    }

    private static bool IsNullable(Type type)
    {
        return !type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
    }

    private static Type UnwrapNullable(Type type)
    {
        if (type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return type.GetGenericArguments()[0];
        return type;
    }

    private sealed class PropertyMapping
    {
        public PropertyInfo Property { get; }
        public string[] ClaimTypes { get; }

        public PropertyMapping(PropertyInfo property, string[] claimTypes)
        {
            Property = property;
            ClaimTypes = claimTypes;
        }
    }
}
