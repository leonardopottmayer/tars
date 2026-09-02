namespace Pottmayer.Tars.Communication.Email.MailKit.Options;

/// <summary>
/// Validation entry point for <see cref="MailKitEmailOptions"/>, wired into the options pipeline by
/// <c>AddTarsMailKitEmailOptions</c> and run on application start.
/// </summary>
internal static class MailKitEmailOptionsValidation
{
    /// <summary>
    /// Validates the bound <see cref="MailKitEmailOptions"/> instance.
    /// </summary>
    /// <param name="options">The options instance to validate.</param>
    /// <returns><c>true</c> when non-null and <see cref="MailKitEmailOptions.IsValid"/>; otherwise <c>false</c>.</returns>
    public static bool Validate(MailKitEmailOptions options)
        => options is not null && options.IsValid();
}
