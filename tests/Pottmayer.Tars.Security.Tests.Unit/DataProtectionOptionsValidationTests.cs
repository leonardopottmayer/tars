using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.DataProtection.DI;
using Pottmayer.Tars.Security.DataProtection.Options;

namespace Pottmayer.Tars.Security.Tests.Unit;

public class DataProtectionOptionsValidationTests
{
    [Fact]
    public void Active_key_version_missing_from_keys_is_rejected_with_the_rule_in_the_message()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{DataProtectionOptions.SectionName}:ActiveKeyVersion"] = "v2",
            [$"{DataProtectionOptions.SectionName}:Keys:v1"] = Convert.ToBase64String(new byte[32]),
        });
        builder.AddTarsDataProtectionOptions();
        using var sp = builder.Services.BuildServiceProvider();

        var act = () => sp.GetRequiredService<IOptions<DataProtectionOptions>>().Value;

        act.Should().Throw<OptionsValidationException>().WithMessage("*ActiveKeyVersion must be one of the Keys*");
    }
}
