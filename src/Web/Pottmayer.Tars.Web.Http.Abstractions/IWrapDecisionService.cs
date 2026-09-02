namespace Pottmayer.Tars.Web.Http.Abstractions;

/// <summary>
/// Decides whether an HTTP response should be wrapped in a Tars envelope.
/// </summary>
public interface IWrapDecisionService
{
    /// <summary>Determines whether the supplied response context should be wrapped.</summary>
    /// <param name="context">The response and endpoint context to evaluate.</param>
    /// <returns><c>true</c> when the response should be wrapped.</returns>
    bool ShouldWrap(WrapDecisionContext context);
}
