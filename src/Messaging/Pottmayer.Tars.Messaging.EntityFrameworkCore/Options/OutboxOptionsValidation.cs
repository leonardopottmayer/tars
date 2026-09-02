namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Options;

/// <summary>
/// Validation entry point for <see cref="OutboxOptions"/>, wired into the options pipeline by
/// <c>AddTarsOutboxOptions</c> and run on application start.
/// </summary>
internal static class OutboxOptionsValidation
{
    /// <summary>
    /// Validates the bound <see cref="OutboxOptions"/> instance.
    /// </summary>
    /// <param name="options">The options instance to validate.</param>
    /// <returns><c>true</c> when non-null and <see cref="OutboxOptions.IsValid"/>; otherwise <c>false</c>.</returns>
    public static bool Validate(OutboxOptions options)
        => options is not null && options.IsValid();
}
