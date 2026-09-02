namespace Pottmayer.Tars.Core.Cqrs.Commands
{
    /// <summary>Base class for a command that returns <see cref="Primitives.Outcomes.Result{TResult}"/>.</summary>
    /// <typeparam name="TResult">Type carried by a successful result.</typeparam>
    public abstract class CommandBase<TResult> : ICommand<TResult>
        where TResult : notnull
    {
        /// <inheritdoc/>
        public ICommandOptions CommandOptions { get; set; } = Commands.CommandOptions.Default();

        /// <summary>Initializes a new command with default options.</summary>
        protected CommandBase() { }
    }

    /// <summary>Base class for a command with an explicit input that returns <see cref="Primitives.Outcomes.Result{TResult}"/>.</summary>
    /// <typeparam name="TInput">Type of the command input payload.</typeparam>
    /// <typeparam name="TResult">Type carried by a successful result.</typeparam>
    public abstract class CommandBase<TInput, TResult> : CommandBase<TResult>
        where TInput : notnull
        where TResult : notnull
    {
        /// <inheritdoc/>
        public TInput Input { get; set; }

        /// <summary>Initializes a new command with the given input and default options.</summary>
        /// <param name="input">The command input payload.</param>
        protected CommandBase(TInput input)
        {
            Input = input;
        }
    }
}
