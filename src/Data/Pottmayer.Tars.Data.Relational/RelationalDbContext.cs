using Microsoft.EntityFrameworkCore;

namespace Pottmayer.Tars.Data.Relational;

/// <summary>
/// Base class for all application DbContexts in the Tars Data.Relational stack.
/// </summary>
public abstract class RelationalDbContext : DbContext
{
    protected RelationalDbContext(DbContextOptions options) : base(options) { }
}
