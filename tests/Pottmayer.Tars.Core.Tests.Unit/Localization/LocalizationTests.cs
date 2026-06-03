using System.Globalization;
using FluentAssertions;
using Pottmayer.Tars.Core.Localization;
using Pottmayer.Tars.Core.Localization.Abstractions;

namespace Pottmayer.Tars.Core.Tests.Unit.Localization;

public class LocalizationTests
{
    private static InMemoryMessageSource Source(Dictionary<string, IDictionary<string, string>> messages)
        => new(messages.ToDictionary(kv => kv.Key, kv => kv.Value));

    private static InMemoryMessageSource StandardSource() => Source(new()
    {
        ["en"] = new Dictionary<string, string> { ["greeting"] = "Hello {0}", ["bye"] = "Bye" },
        ["pt-BR"] = new Dictionary<string, string> { ["greeting"] = "Olá {0}" },
    });

    [Fact]
    public void InMemorySource_returns_exact_culture_match()
    {
        StandardSource().TryGet("greeting", new CultureInfo("pt-BR")).Should().Be("Olá {0}");
    }

    [Fact]
    public void InMemorySource_falls_back_to_neutral_then_english()
    {
        // pt-PT not present -> neutral "pt" not present -> english fallback
        StandardSource().TryGet("bye", new CultureInfo("pt-PT")).Should().Be("Bye");
    }

    [Fact]
    public void InMemorySource_returns_null_when_missing_everywhere()
    {
        StandardSource().TryGet("unknown", new CultureInfo("en")).Should().BeNull();
    }

    [Fact]
    public void CompositeProvider_resolves_first_source_that_has_the_key()
    {
        var first = Source(new() { ["en"] = new Dictionary<string, string> { ["k"] = "from-first" } });
        var second = Source(new() { ["en"] = new Dictionary<string, string> { ["k"] = "from-second" } });
        var provider = new CompositeMessageProvider([first, second]);

        provider.Get("k", new CultureInfo("en")).Should().Be("from-first");
    }

    [Fact]
    public void CompositeProvider_uses_fallback_then_key_when_unresolved()
    {
        var provider = new CompositeMessageProvider([StandardSource()]);

        provider.Get("missing", new CultureInfo("en"), fallback: "fb").Should().Be("fb");
        provider.Get("missing", new CultureInfo("en")).Should().Be("missing");
    }

    [Fact]
    public void CompositeProvider_formats_with_args()
    {
        var provider = new CompositeMessageProvider([StandardSource()]);

        provider.Get("greeting", new CultureInfo("en"), fallback: null, "World").Should().Be("Hello World");
    }
}
