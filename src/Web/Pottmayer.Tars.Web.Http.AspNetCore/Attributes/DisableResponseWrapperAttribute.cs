using Microsoft.AspNetCore.Mvc.Filters;

namespace Pottmayer.Tars.Web.Http.AspNetCore.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class DisableResponseWrapperAttribute : Attribute, IFilterMetadata { }
