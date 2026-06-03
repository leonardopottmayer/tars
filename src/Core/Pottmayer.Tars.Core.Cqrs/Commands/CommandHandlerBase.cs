using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.Commands
{
    public abstract class CommandHandlerBase<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
        where TResult : notnull
    {
        protected CommandHandlerBase() { }

        public ValueTask<Result<TResult>> Handle(TCommand request, CancellationToken ct = default)
        {
            return new ValueTask<Result<TResult>>(HandleAsync(request, ct));
        }

        protected Result<TResult> Ok(TResult value, string? correlationId = null)
            => Result<TResult>.Success(value, correlationId);

        protected Result<TResult> Fail(params Error[] errors)
            => Result<TResult>.Failure(errors);

        protected Result<TResult> Fail(IEnumerable<Error> errors, string? correlationId = null)
            => Result<TResult>.Failure(errors, correlationId);

        protected abstract Task<Result<TResult>> HandleAsync(TCommand request, CancellationToken ct = default);
    }
}
