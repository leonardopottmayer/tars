namespace Pottmayer.Tars.Communication.Email.Abstractions;

/// <summary>Outcome of a successful delivery: the provider that accepted it and its message id, if any.</summary>
public sealed record EmailDeliveryResult(string Provider, string? ProviderMessageId);
