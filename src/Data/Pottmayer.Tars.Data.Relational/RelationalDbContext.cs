using Microsoft.EntityFrameworkCore;

namespace Pottmayer.Tars.Data.Relational;

/// <summary>
/// Base class for all application DbContexts in the Tars Data.Relational stack.
/// </summary>
public abstract class RelationalDbContext : DbContext
{
    /// <summary>Initializes the context with the given EF Core options.</summary>
    /// <param name="options">The DbContext options.</param>
    protected RelationalDbContext(DbContextOptions options) : base(options) { }
}
