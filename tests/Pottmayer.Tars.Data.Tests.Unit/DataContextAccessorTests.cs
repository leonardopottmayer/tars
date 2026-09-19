using FluentAssertions;
using Pottmayer.Tars.Data.DataContext;
using Pottmayer.Tars.Data.Tests.Unit.Infrastructure;

namespace Pottmayer.Tars.Data.Tests.Unit;

/// <summary>
/// The ambient context is keyed by database role, so one flow can hold a context per key at once —
/// what makes multi-database (and mixed-provider) access work within a single unit of work.
/// </summary>
public class DataContextAccessorTests
{
    [Fact]
    public void Keyed_slots_are_independent()
    {
        var accessor = new DataContextAccessor();
        var sql = new FakeDataContext("sql");
        var mongo = new FakeDataContext("mongo");

        accessor.SetCurrent("central", sql);
        accessor.SetCurrent("catalog", mongo);

        accessor.GetCurrent("central").Should().BeSameAs(sql);
        accessor.GetCurrent("catalog").Should().BeSameAs(mongo);
    }

    [Fact]
    public void Clearing_a_key_removes_only_that_slot()
    {
        var accessor = new DataContextAccessor();
        accessor.SetCurrent("central", new FakeDataContext("sql"));
        accessor.SetCurrent("catalog", new FakeDataContext("mongo"));

        accessor.SetCurrent("central", null);

        accessor.GetCurrent("central").Should().BeNull();
        accessor.GetCurrent("catalog").Should().NotBeNull();
    }

    [Fact]
    public void Non_keyed_current_is_separate_from_keyed_slots()
    {
        var accessor = new DataContextAccessor();
        var ambient = new FakeDataContext("ambient");

        accessor.SetCurrent(ambient);

        accessor.Current.Should().BeSameAs(ambient);
        accessor.GetCurrent("central").Should().BeNull();
    }
}
