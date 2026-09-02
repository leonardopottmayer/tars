namespace Pottmayer.Tars.Core.Cqrs.Commands
{
    /// <summary>Default <see cref="ICommandOptions"/> implementation.</summary>
    public class CommandOptions : ICommandOptions
    {
        /// <summary>Initializes a new instance with default settings.</summary>
        public CommandOptions() { }

        /// <summary>Creates a new instance with default settings.</summary>
        /// <returns>A new <see cref="CommandOptions"/>.</returns>
        public static CommandOptions New() => new();

        /// <summary>Returns the default options used when a command specifies none.</summary>
        /// <returns>A new <see cref="CommandOptions"/>.</returns>
        public static CommandOptions Default() => New();
    }
}
