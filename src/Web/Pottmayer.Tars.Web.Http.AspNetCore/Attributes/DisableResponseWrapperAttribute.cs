using Microsoft.AspNetCore.Mvc.Filters;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
/// <summary>
/// Explicitly disables response wrapping for a controller or action.
/// </summary>
public sealed class DisableResponseWrapperAttribute : Attribute, IFilterMetadata { }
