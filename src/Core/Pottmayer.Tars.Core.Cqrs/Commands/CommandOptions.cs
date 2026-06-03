namespace Pottmayer.Tars.Core.Cqrs.Commands
{
    public class CommandOptions : ICommandOptions
    {
        public CommandOptions() { }

        public static CommandOptions New() => new();

        public static CommandOptions Default() => New();
    }
}
