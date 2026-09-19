using System.Linq.Expressions;
using System.Reflection;

namespace Pottmayer.Tars.Data.Query;

/// <summary>
/// Provider-agnostic <see cref="IQueryable{T}"/> ordering helpers built from a property named at runtime.
/// Because they only compose standard <see cref="Queryable"/> calls, they work against any LINQ provider
/// (EF Core, MongoDB driver). Materialization (count/list) is provider-specific and lives in each provider.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>Orders the queryable by a property named at runtime.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="propertyName">Name of the property to order by.</param>
    /// <param name="ascending">Whether to order ascending.</param>
    /// <returns>The ordered queryable.</returns>
    public static IOrderedQueryable<T> OrderByProperty<T>(this IQueryable<T> source, string propertyName, bool ascending)
    {
        var (param, access) = PropertyAccess(typeof(T), propertyName);
        var lambda = Expression.Lambda(access, param);
        var method = ascending ? "OrderBy" : "OrderByDescending";
        var call = Expression.Call(typeof(Queryable), method, [typeof(T), access.Type],
            source.Expression, Expression.Quote(lambda));
        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }

    /// <summary>Adds a secondary ordering to the queryable by a property named at runtime.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The already-ordered queryable.</param>
    /// <param name="propertyName">Name of the property to order by.</param>
    /// <param name="ascending">Whether to order ascending.</param>
    /// <returns>The ordered queryable.</returns>
    public static IOrderedQueryable<T> ThenByProperty<T>(this IOrderedQueryable<T> source, string propertyName, bool ascending)
    {
        var (param, access) = PropertyAccess(typeof(T), propertyName);
        var lambda = Expression.Lambda(access, param);
        var method = ascending ? "ThenBy" : "ThenByDescending";
        var call = Expression.Call(typeof(Queryable), method, [typeof(T), access.Type],
            source.Expression, Expression.Quote(lambda));
        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }

    /// <summary>Builds the parameter and member-access expressions for a property named at runtime.</summary>
    private static (ParameterExpression, MemberExpression) PropertyAccess(Type entityType, string name)
    {
        var param = Expression.Parameter(entityType, "x");
        var prop = GetProperty(entityType, name)
            ?? throw new ArgumentException($"Property '{name}' not found on '{entityType.Name}'.", nameof(name));
        return (param, Expression.Property(param, prop));
    }

    /// <summary>Finds a public instance property by name (case-insensitive), walking the base types.</summary>
    private static PropertyInfo? GetProperty(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly;
        for (var t = type; t != null; t = t.BaseType)
        {
            var p = t.GetProperty(name, flags);
            if (p is not null) return p;
        }
        return null;
    }
}
