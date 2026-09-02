namespace Pottmayer.Tars.Communication.Telegram.Options;

/// <summary>
/// Validation entry point for <see cref="TelegramOptions"/>, wired into the options pipeline by
/// <c>AddTarsTelegramOptions</c> and run on application start.
/// </summary>
internal static class TelegramOptionsValidation
{
    /// <summary>
    /// Validates the bound <see cref="TelegramOptions"/> instance.
    /// </summary>
    /// <param name="options">The options instance to validate.</param>
    /// <returns><c>true</c> when non-null and <see cref="TelegramOptions.IsValid"/>; otherwise <c>false</c>.</returns>
    public static bool Validate(TelegramOptions options)
        => options is not null && options.IsValid();
}
