using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.DI;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class IdentityOptionsValidationTests
{
    private static Func<SecurityIdentityOptions> Resolve(string signingKey)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{SecurityIdentityOptions.SectionName}:Jwt:SigningKey"] = signingKey,
        });
        builder.AddTarsIdentityOptions();
        var sp = builder.Services.BuildServiceProvider();
        return () => sp.GetRequiredService<IOptions<SecurityIdentityOptions>>().Value;
    }

    [Fact]
    public void Signing_key_shorter_than_32_bytes_is_rejected()
        => Resolve(new string('k', 31)).Should().Throw<OptionsValidationException>();

    [Fact]
    public void Signing_key_of_32_bytes_is_accepted()
        => Resolve(new string('k', 32)).Should().NotThrow();
}
