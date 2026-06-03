using FluentAssertions;
using Pottmayer.Tars.Web.Http;
using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Tests.Unit;

public class WrapDecisionServiceTests
{
    private readonly WrapDecisionService _sut = new();

    [Fact]
    public void Does_not_wrap_when_wrapping_disabled()
    {
        _sut.ShouldWrap(new WrapDecisionContext { WrappingEnabled = false, IsExplicitEnabled = true })
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(true, false, false)]   // file/stream
    [InlineData(false, true, false)]   // already wrapped
    public void Does_not_wrap_file_stream_or_already_wrapped(bool fileOrStream, bool alreadyWrapped, bool _)
    {
        _sut.ShouldWrap(new WrapDecisionContext
        {
            IsFileOrStream = fileOrStream,
            IsAlreadyWrapped = alreadyWrapped,
            ControllersDefaultMode = ControllersWrappingMode.WrapAll,
        }).Should().BeFalse();
    }

    [Fact]
    public void Explicit_disabled_wins_over_enabled_defaults()
    {
        _sut.ShouldWrap(new WrapDecisionContext
        {
            IsExplicitDisabled = true,
            ControllersDefaultMode = ControllersWrappingMode.WrapAll,
        }).Should().BeFalse();
    }

    [Fact]
    public void Minimal_api_opt_in_wraps()
    {
        _sut.ShouldWrap(new WrapDecisionContext { MinimalApiOptIn = true }).Should().BeTrue();
    }

    [Fact]
    public void Explicit_enabled_wraps()
    {
        _sut.ShouldWrap(new WrapDecisionContext { IsExplicitEnabled = true }).Should().BeTrue();
    }

    [Theory]
    [InlineData(ControllersWrappingMode.WrapAll, true)]
    [InlineData(ControllersWrappingMode.WrapNone, false)]
    public void Falls_back_to_controllers_default_mode(ControllersWrappingMode mode, bool expected)
    {
        _sut.ShouldWrap(new WrapDecisionContext { ControllersDefaultMode = mode })
            .Should().Be(expected);
    }
}
