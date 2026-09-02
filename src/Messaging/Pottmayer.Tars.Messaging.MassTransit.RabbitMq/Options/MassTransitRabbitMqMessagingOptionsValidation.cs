namespace Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Options;

/// <summary>
/// Validation entry point for <see cref="MassTransitRabbitMqMessagingOptions"/>, wired into the options pipeline by
/// <c>AddTarsRabbitMqOptions</c> and run on application start.
/// </summary>
internal static class MassTransitRabbitMqMessagingOptionsValidation
{
    /// <summary>
    /// Validates the bound <see cref="MassTransitRabbitMqMessagingOptions"/> instance.
    /// </summary>
    /// <param name="options">The options instance to validate.</param>
    /// <returns><c>true</c> when non-null and <see cref="MassTransitRabbitMqMessagingOptions.IsValid"/>; otherwise <c>false</c>.</returns>
    public static bool Validate(MassTransitRabbitMqMessagingOptions options)
        => options is not null && options.IsValid();
}
