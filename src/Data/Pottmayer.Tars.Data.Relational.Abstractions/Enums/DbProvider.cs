namespace Pottmayer.Tars.Data.Relational.Abstractions.Enums;

/// <summary>Supported relational database providers.</summary>
public enum DbProvider
{
    /// <summary>PostgreSQL.</summary>
    PostgreSQL,

    /// <summary>MySQL.</summary>
    MySql,

    /// <summary>Oracle Database.</summary>
    Oracle,

    /// <summary>SQLite.</summary>
    Sqlite,

    /// <summary>Microsoft SQL Server.</summary>
    SqlServer,

    /// <summary>Unknown or unspecified provider.</summary>
    Unknown
}
