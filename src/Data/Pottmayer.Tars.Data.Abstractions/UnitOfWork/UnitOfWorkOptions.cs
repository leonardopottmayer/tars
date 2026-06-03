namespace Pottmayer.Tars.Data.Abstractions.UnitOfWork;

public sealed class UnitOfWorkOptions
{
    /// <summary>When true (default), <c>CommitAsync</c> is called automatically after the delegate succeeds.</summary>
    public bool CommitOnSuccess { get; init; } = true;
}
