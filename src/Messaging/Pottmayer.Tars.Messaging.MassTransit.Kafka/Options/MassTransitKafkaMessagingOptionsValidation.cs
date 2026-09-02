namespace Pottmayer.Tars.Messaging.MassTransit.Kafka.Options;

/// <summary>
/// Validation entry point for <see cref="MassTransitKafkaMessagingOptions"/>, wired into the options pipeline by
/// <c>AddTarsKafkaOptions</c> and run on application start.
/// </summary>
internal static class MassTransitKafkaMessagingOptionsValidation
{
    /// <summary>
    /// Validates the bound <see cref="MassTransitKafkaMessagingOptions"/> instance.
    /// </summary>
    /// <param name="options">The options instance to validate.</param>
    /// <returns><c>true</c> when non-null and <see cref="MassTransitKafkaMessagingOptions.IsValid"/>; otherwise <c>false</c>.</returns>
    public static bool Validate(MassTransitKafkaMessagingOptions options)
        => options is not null && options.IsValid();
}
