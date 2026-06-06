using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Communication.Email.MailKit.Options;

namespace Pottmayer.Tars.Communication.Email.MailKit.DI;

public static class MailKitEmailOptionsDI
{
    /// <summary>
    /// Binds <see cref="MailKitEmailOptions"/> from configuration (default section
    /// <c>Tars:Communication:Email:Smtp</c>).
    /// </summary>
    public static OptionsBuilder<MailKitEmailOptions> AddTarsMailKitEmailOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<MailKitEmailOptions>? configure = null)
    {
        sectionName ??= MailKitEmailOptions.SectionName;

        var ob = builder.Services
            .AddOptions<MailKitEmailOptions>()
            .Bind(builder.Configuration.GetSection(sectionName));

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
