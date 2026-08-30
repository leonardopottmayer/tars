namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// Where the outbox table lives. Shared by the EF configuration and the DDL script so the mapping and
/// the migration can never drift on table or column names.
/// </summary>
public static class OutboxStorage
{
    /// <summary>Default table name. Prefixed <c>tars_</c> so it reads as framework plumbing next to domain tables.</summary>
    public const string DefaultTable = "tars_outbox_message";

    // Column names, in one place. The EF configuration maps to these and the shipped .sql creates them.
    public const string IdColumn = "id";
    public const string EventIdColumn = "event_id";
    public const string EventTypeColumn = "event_type";
    public const string VersionColumn = "event_version";
    public const string PayloadColumn = "payload";
    public const string HeadersColumn = "headers";
    public const string OccurredAtColumn = "occurred_at";
    public const string CreatedAtColumn = "created_at";
    public const string StatusColumn = "status";
    public const string AttemptsColumn = "attempts";
    public const string NextAttemptAtColumn = "next_attempt_at";
    public const string ProcessedAtColumn = "processed_at";
    public const string ErrorColumn = "error";
}
