using FluentAssertions;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

namespace Pottmayer.Tars.Messaging.Tests.Unit.Outbox;

public class JsonIntegrationEventSerializerTests
{
    private readonly JsonIntegrationEventSerializer _serializer = new();

    [Fact]
    public void Payload_round_trips_through_the_resolved_type()
    {
        var original = new ThingHappened(Guid.NewGuid(), DateTimeOffset.UtcNow, "hello");

        var payload = _serializer.SerializePayload(original);
        var restored = _serializer.DeserializePayload(typeof(ThingHappened), payload);

        restored.Should().BeOfType<ThingHappened>().Which.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Headers_are_null_when_the_event_carries_none()
    {
        var plain = new ThingHappened(Guid.NewGuid(), DateTimeOffset.UtcNow, "x");
        _serializer.SerializeHeaders(plain).Should().BeNull();

        var emptyHeaders = new HeaderedThing(Guid.NewGuid(), DateTimeOffset.UtcNow, "x");
        _serializer.SerializeHeaders(emptyHeaders).Should().BeNull("empty headers are not worth a column value");
    }

    [Fact]
    public void Headers_serialize_when_present()
    {
        var headered = new HeaderedThing(Guid.NewGuid(), DateTimeOffset.UtcNow, "x")
        {
            Headers = new Dictionary<string, string> { ["tars.tenant-id"] = "acme" }
        };

        _serializer.SerializeHeaders(headered).Should().Contain("tars.tenant-id").And.Contain("acme");
    }
}
