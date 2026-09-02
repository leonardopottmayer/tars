namespace Pottmayer.Tars.Communication.Email.Abstractions;

/// <summary>Outcome of a successful delivery: the provider that accepted it and its message id, if any.</summary>
/// <param name="Provider">Name of the transport that accepted the message.</param>
/// <param name="ProviderMessageId">Provider-assigned message identifier, when the transport returns one.</param>
public sealed record EmailDeliveryResult(string Provider, string? ProviderMessageId);
