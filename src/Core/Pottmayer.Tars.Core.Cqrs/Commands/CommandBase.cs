namespace Pottmayer.Tars.Core.Cqrs.Commands
{
    public abstract class CommandBase<TResult> : ICommand<TResult>
        where TResult : notnull
    {
        public ICommandOptions CommandOptions { get; set; } = Commands.CommandOptions.Default();

        protected CommandBase() { }
    }

    public abstract class CommandBase<TInput, TResult> : CommandBase<TResult>
        where TInput : notnull
        where TResult : notnull
    {
        public TInput Input { get; set; }

        protected CommandBase(TInput input)
        {
            Input = input;
        }
    }
}
