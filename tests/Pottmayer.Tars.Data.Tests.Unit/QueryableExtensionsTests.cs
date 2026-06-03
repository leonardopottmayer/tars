using FluentAssertions;
using Pottmayer.Tars.Data.Relational.Extensions;

namespace Pottmayer.Tars.Data.Tests.Unit;

public class QueryableExtensionsTests
{
    private sealed record Person(string Name, int Age);

    private static readonly IQueryable<Person> People = new[]
    {
        new Person("Carol", 30),
        new Person("Alice", 30),
        new Person("Bob", 25),
    }.AsQueryable();

    [Fact]
    public void OrderByProperty_ascending_sorts_by_named_property()
    {
        var ordered = People.OrderByProperty("Age", ascending: true).ToList();

        ordered.Select(p => p.Age).Should().Equal(25, 30, 30);
    }

    [Fact]
    public void OrderByProperty_descending_sorts_by_named_property()
    {
        var ordered = People.OrderByProperty("Age", ascending: false).ToList();

        ordered.First().Age.Should().Be(30);
        ordered.Last().Age.Should().Be(25);
    }

    [Fact]
    public void ThenByProperty_breaks_ties()
    {
        var ordered = People
            .OrderByProperty("Age", ascending: true)
            .ThenByProperty("Name", ascending: true)
            .ToList();

        ordered.Select(p => p.Name).Should().Equal("Bob", "Alice", "Carol");
    }

    [Fact]
    public void OrderByProperty_is_case_insensitive_on_property_name()
    {
        var ordered = People.OrderByProperty("age", ascending: true).ToList();

        ordered.First().Age.Should().Be(25);
    }

    [Fact]
    public void OrderByProperty_unknown_property_throws()
    {
        var act = () => People.OrderByProperty("Missing", ascending: true).ToList();

        act.Should().Throw<ArgumentException>();
    }
}
