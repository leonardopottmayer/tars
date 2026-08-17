using Pottmayer.Tars.Messaging.Abstractions;

// Two events with the same type name in different namespaces. Neither declares
// [IntegrationEventName], so both fall back to convention and collide on "clash" — the case the
// registry has to reject at startup instead of silently dropping one of them.
//
// Internal on purpose: an assembly scan uses GetExportedTypes, and a public pair here would make
// every scan of this test assembly throw. Tests that need them pass the types explicitly.

namespace Duplicated
{
    internal sealed record Clash(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;
}

namespace AlsoDuplicated
{
    internal sealed record Clash(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;
}
