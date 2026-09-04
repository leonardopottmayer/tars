using FluentAssertions;
using Pottmayer.Tars.Ai.Chat.Gemini.Options;

namespace Pottmayer.Tars.Ai.Chat.Tests.Unit.Gemini;

public class GeminiAiOptionsTests
{
    [Fact]
    public void SectionName_follows_the_framework_convention()
        => GeminiAiOptions.SectionName.Should().Be("Tars:Ai:Chat:Gemini");

    [Fact]
    public void IsValid_accepts_defaults_without_an_api_key()
        => new GeminiAiOptions().IsValid().Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("/relative/only")]
    public void IsValid_rejects_a_non_absolute_base_url(string baseUrl)
        => new GeminiAiOptions { BaseUrl = baseUrl }.IsValid().Should().BeFalse();

    [Fact]
    public void IsValid_rejects_a_non_positive_timeout()
        => new GeminiAiOptions { RequestTimeout = TimeSpan.Zero }.IsValid().Should().BeFalse();

    [Fact]
    public void Validation_delegate_rejects_null()
        => GeminiAiOptionsValidation.Validate(null!).Should().BeFalse();
}
