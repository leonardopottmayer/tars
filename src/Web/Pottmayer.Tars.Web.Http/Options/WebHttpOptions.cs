namespace Pottmayer.Tars.Web.Http.Options;

public sealed class WebHttpOptions
{
    public const string SectionName = "Tars:Web:Http";
    public const string ValidationErrorMessage = "Invalid WebHttpOptions.";

    public bool Enabled { get; init; } = true;
    public bool IncludeTraceId { get; init; } = false;

    public bool IsValid() => true;
}
