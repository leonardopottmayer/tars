namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// The slice of a claimed outbox row the relay needs to deliver it: enough to resolve the type,
/// deserialize the body, and later find the row again to record the outcome. Deliberately not the full
/// <see cref="OutboxMessage"/> — the lease reads through raw SQL (to take the row lock), so it returns a
/// plain snapshot rather than a change-tracked entity.
/// </summary>
public sealed record LeasedOutboxMessage(Guid Id, string EventType, string Payload);
