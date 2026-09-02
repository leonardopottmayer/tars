using Pottmayer.Tars.Core.Primitives.Outcomes;

namespace Pottmayer.Tars.Core.Cqrs.Commands
{
    /// <summary>
    /// Base class for command handlers. Adapts the mediator's <see cref="Handle"/> entry point onto the
    /// async <see cref="HandleAsync"/> template method and offers helpers for building success/failure results.
    /// </summary>
    /// <typeparam name="TCommand">The command type handled.</typeparam>
    /// <typeparam name="TResult">Type carried by a successful result.</typeparam>
    public abstract class CommandHandlerBase<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
        where TResult : notnull
    {
        /// <summary>Initializes the handler base.</summary>
        protected CommandHandlerBase() { }

        /// <inheritdoc/>
        public ValueTask<Result<TResult>> Handle(TCommand request, CancellationToken ct = default)
        {
            return new ValueTask<Result<TResult>>(HandleAsync(request, ct));
        }

        /// <summary>Builds a successful result carrying <paramref name="value"/>.</summary>
        /// <param name="value">The value produced by the command.</param>
        /// <param name="correlationId">Optional correlation id to attach to the result.</param>
        /// <returns>A successful <see cref="Result{TResult}"/>.</returns>
        protected Result<TResult> Ok(TResult value, string? correlationId = null)
            => Result<TResult>.Success(value, correlationId);

        /// <summary>Builds a failed result from the given errors.</summary>
        /// <param name="errors">The errors describing the failure.</param>
        /// <returns>A failed <see cref="Result{TResult}"/>.</returns>
        protected Result<TResult> Fail(params Error[] errors)
            => Result<TResult>.Failure(errors);

        /// <summary>Builds a failed result from the given errors.</summary>
        /// <param name="errors">The errors describing the failure.</param>
        /// <param name="correlationId">Optional correlation id to attach to the result.</param>
        /// <returns>A failed <see cref="Result{TResult}"/>.</returns>
        protected Result<TResult> Fail(IEnumerable<Error> errors, string? correlationId = null)
            => Result<TResult>.Failure(errors, correlationId);

        /// <summary>Handles the command. Implemented by concrete handlers.</summary>
        /// <param name="request">The command to handle.</param>
        /// <param name="ct">Token used to cancel handling.</param>
        /// <returns>The outcome of handling the command.</returns>
        protected abstract Task<Result<TResult>> HandleAsync(TCommand request, CancellationToken ct = default);
    }
}
