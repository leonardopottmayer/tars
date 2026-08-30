namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// Lifecycle of a row in the transactional outbox. Stored as a small integer so the relay's "due"
/// query can filter on it with an index; the numeric values are part of the on-disk contract and must
/// not be renumbered.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>Written and awaiting delivery. The relay picks these up once <c>NextAttemptAt</c> is due.</summary>
    Pending = 0,

    /// <summary>Handed to the local handlers successfully. Kept for a while for audit, then purged.</summary>
    Dispatched = 1,

    /// <summary>Failed past the retry budget. No longer delivered; kept for inspection and manual replay.</summary>
    Dead = 2
}
