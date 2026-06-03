using FluentAssertions;
using Pottmayer.Tars.Caching.Core;

namespace Pottmayer.Tars.Caching.Tests.Unit;

public class SystemTextJsonCacheSerializerTests
{
    private sealed record Sample(int Id, string Name);

    [Fact]
    public void Roundtrips_an_object()
    {
        var serializer = new SystemTextJsonCacheSerializer();
        var value = new Sample(1, "tars");

        var bytes = serializer.Serialize(value);
        var restored = serializer.Deserialize<Sample>(bytes);

        restored.Should().Be(value);
    }

    [Fact]
    public void Uses_web_defaults_camelCase()
    {
        var serializer = new SystemTextJsonCacheSerializer();

        var json = System.Text.Encoding.UTF8.GetString(serializer.Serialize(new Sample(1, "x")));

        json.Should().Contain("\"id\"").And.Contain("\"name\"");
    }
}
