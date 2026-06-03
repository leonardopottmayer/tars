using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Pottmayer.Tars.Data.Abstractions.Query;

namespace Pottmayer.Tars.Data.Relational.Extensions;

/// <summary>
/// Converts <see cref="QueryParams"/> to <see cref="DataQueryParams{TEntity}"/>
/// with whitelist validation and expression building.
/// </summary>
public static class QueryParamsExtensions
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static DataQueryParams<TEntity>? ToDataQueryParams<TEntity>(
        this QueryParams? queryParams,
        IReadOnlySet<string> allowedPropertyNames)
        where TEntity : class
    {
        if (queryParams is null) return null;

        Expression<Func<TEntity, bool>>? predicate = null;
        if (queryParams.Filters is { Count: > 0 } filters && allowedPropertyNames.Count > 0)
        {
            var param = Expression.Parameter(typeof(TEntity), "x");
            Expression? combined = null;
            foreach (var spec in filters)
            {
                var field = spec.Field?.Trim();
                if (string.IsNullOrEmpty(field)) continue;
                if (!allowedPropertyNames.Contains(field, StringComparer.OrdinalIgnoreCase)) continue;

                var prop = GetProperty(typeof(TEntity), field);
                if (prop is null) continue;

                var expr = BuildExpression(param, prop, spec);
                if (expr is null) continue;
                combined = combined is null ? expr : Expression.AndAlso(combined, expr);
            }
            if (combined is not null)
                predicate = Expression.Lambda<Func<TEntity, bool>>(combined, param);
        }

        IReadOnlyList<SortOption>? orderBy = null;
        if (queryParams.OrderBy is { Count: > 0 } sortList && allowedPropertyNames.Count > 0)
        {
            var allowed = sortList
                .Where(s => !string.IsNullOrEmpty(s.PropertyName) &&
                            allowedPropertyNames.Contains(s.PropertyName, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (allowed.Count > 0)
                orderBy = allowed;
        }

        int? skip = null, take = null;
        if (queryParams.Paged)
        {
            skip = (queryParams.Page - 1) * queryParams.PageSize;
            take = queryParams.PageSize;
        }

        return new DataQueryParams<TEntity>
        {
            Predicate = predicate,
            Skip = skip,
            Take = take,
            OrderBy = orderBy
        };
    }

    private static Expression? BuildExpression(ParameterExpression param, PropertyInfo prop, FilterSpec spec)
    {
        var access = Expression.Property(param, prop);
        var propType = prop.PropertyType;
        var parsed = ParseValue(spec.Value, propType);

        return spec.Operator switch
        {
            FilterOperator.Eq => BuildBinary(access, parsed, propType, Expression.Equal),
            FilterOperator.NotEq => BuildBinary(access, parsed, propType, Expression.NotEqual),
            FilterOperator.Gt => parsed is null ? null : BuildBinary(access, parsed, propType, Expression.GreaterThan),
            FilterOperator.Gte => parsed is null ? null : BuildBinary(access, parsed, propType, Expression.GreaterThanOrEqual),
            FilterOperator.Lt => parsed is null ? null : BuildBinary(access, parsed, propType, Expression.LessThan),
            FilterOperator.Lte => parsed is null ? null : BuildBinary(access, parsed, propType, Expression.LessThanOrEqual),
            FilterOperator.Contains => BuildStringMethod(access, parsed?.ToString(), propType, "Contains"),
            FilterOperator.StartsWith => BuildStringMethod(access, parsed?.ToString(), propType, "StartsWith"),
            FilterOperator.EndsWith => BuildStringMethod(access, parsed?.ToString(), propType, "EndsWith"),
            FilterOperator.In => BuildIn(param, prop, spec, propType),
            _ => null
        };
    }

    private static Expression? BuildBinary(MemberExpression access, object? value, Type propType,
        Func<Expression, Expression, BinaryExpression> op)
    {
        if (value is null)
        {
            if (propType.IsValueType && Nullable.GetUnderlyingType(propType) is null) return null;
            return op(access, Expression.Constant(null, propType));
        }
        return op(access, Expression.Constant(value, propType));
    }

    private static Expression? BuildStringMethod(MemberExpression access, string? value, Type propType, string methodName)
    {
        if (propType != typeof(string) || string.IsNullOrEmpty(value)) return null;
        var method = typeof(string).GetMethod(methodName, [typeof(string)])!;
        return Expression.Call(access, method, Expression.Constant(value));
    }

    private static Expression? BuildIn(ParameterExpression param, PropertyInfo prop, FilterSpec spec, Type propType)
    {
        var items = new List<object?>();
        if (spec.Values is { Count: > 0 } values)
        {
            foreach (var v in values)
            {
                var p = ParseValue(v, propType);
                if (p is not null) items.Add(p);
            }
        }
        else if (spec.Value is string s && s.Contains(','))
        {
            foreach (var part in s.Split(','))
            {
                var p = ParseValue(part.Trim(), propType);
                if (p is not null) items.Add(p);
            }
        }
        else
        {
            var p = ParseValue(spec.Value, propType);
            if (p is not null) items.Add(p);
        }

        if (items.Count == 0) return null;
        var access = Expression.Property(param, prop);
        Expression? expr = null;
        foreach (var item in items)
        {
            var eq = Expression.Equal(access, Expression.Constant(item, propType));
            expr = expr is null ? eq : Expression.OrElse(expr, eq);
        }
        return expr;
    }

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

    private static object? ParseValue(object? value, Type targetType)
    {
        if (value is null)
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null ? null : value;
        var s = value.ToString();
        if (string.IsNullOrEmpty(s)) return null;
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        try
        {
            if (type == typeof(string)) return s;
            if (type == typeof(int)) return int.TryParse(s, Culture, out var i) ? i : (object?)null;
            if (type == typeof(long)) return long.TryParse(s, Culture, out var l) ? l : (object?)null;
            if (type == typeof(bool)) return bool.TryParse(s, out var b) ? b : s == "1";
            if (type == typeof(DateTime)) return DateTime.TryParse(s, Culture, out var dt) ? dt : (object?)null;
            if (type == typeof(DateTimeOffset)) return DateTimeOffset.TryParse(s, Culture, out var dto) ? dto : (object?)null;
            if (type == typeof(Guid)) return Guid.TryParse(s, out var g) ? g : (object?)null;
            if (type.IsEnum) return Enum.TryParse(type, s, true, out var e) ? e : null;
            return Convert.ChangeType(value, type, Culture);
        }
        catch { return null; }
    }
}
