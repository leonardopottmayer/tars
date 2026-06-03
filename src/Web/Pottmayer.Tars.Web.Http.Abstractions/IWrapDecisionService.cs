namespace Pottmayer.Tars.Web.Http.Abstractions;

public interface IWrapDecisionService
{
    bool ShouldWrap(WrapDecisionContext context);
}
