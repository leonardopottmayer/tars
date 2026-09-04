namespace Pottmayer.Tars.Ai.Abstractions;

/// <summary>
/// An AI provider call failed. <see cref="IsPermanent"/> is the field callers act on: a permanent
/// failure will fail identically on a retry (the model name is wrong, the request is malformed), so
/// repeating it only burns attempts; a transient one (endpoint unreachable, the server had a bad
/// moment) may succeed if tried again. Shared across AI capabilities (chat, embeddings, …).
/// </summary>
public sealed class AiException : Exception
{
    public AiException(
        string provider,
        string message,
        bool isPermanent,
        string? model = null,
        int? statusCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Provider = provider;
        IsPermanent = isPermanent;
        Model = model;
        StatusCode = statusCode;
    }

    /// <summary>The provider that failed, e.g. <c>openai</c>.</summary>
    public string Provider { get; }

    /// <summary>True when retrying cannot succeed. The caller should stop and record the failure.</summary>
    public bool IsPermanent { get; }

    /// <summary>The model the request named, when known.</summary>
    public string? Model { get; }

    /// <summary>The HTTP status behind the failure, when it came from the server rather than the wire.</summary>
    public int? StatusCode { get; }
}
