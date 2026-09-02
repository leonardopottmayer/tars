namespace Pottmayer.Tars.Messaging.Broker.Options;

/// <summary>
/// Validation entry point for <see cref="BrokerMessagingOptions"/>, wired into the options pipeline by
/// <c>AddTarsBrokerMessagingOptions</c> and run on application start.
/// </summary>
internal static class BrokerMessagingOptionsValidation
{
    /// <summary>
    /// Validates the bound <see cref="BrokerMessagingOptions"/> instance.
    /// </summary>
    /// <param name="options">The options instance to validate.</param>
    /// <returns><c>true</c> when non-null and <see cref="BrokerMessagingOptions.IsValid"/>; otherwise <c>false</c>.</returns>
    public static bool Validate(BrokerMessagingOptions options)
        => options is not null && options.IsValid();
}
