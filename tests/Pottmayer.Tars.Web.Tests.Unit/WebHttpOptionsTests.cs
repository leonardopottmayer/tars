using FluentAssertions;
using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.AspNetCore.Options;
using Pottmayer.Tars.Web.Http.Options;

namespace Pottmayer.Tars.Web.Tests.Unit;

public class WebHttpOptionsTests
{
    [Fact]
    public void Core_options_are_valid()
    {
        new WebHttpOptions().IsValid().Should().BeTrue();
    }

    [Fact]
    public void AspNetCore_options_reject_unknown_controllers_wrapping_mode()
    {
        new WebHttpAspNetCoreOptions
        {
            ControllersDefaultMode = (ControllersWrappingMode)99
        }.IsValid().Should().BeFalse();
    }

    [Fact]
    public void AspNetCore_options_use_their_own_configuration_section()
    {
        WebHttpAspNetCoreOptions.SectionName.Should().Be("Tars:Web:Http:AspNetCore");
    }
}
