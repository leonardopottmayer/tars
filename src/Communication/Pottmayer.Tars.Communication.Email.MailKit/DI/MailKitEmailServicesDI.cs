using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Communication.Email.Abstractions;

namespace Pottmayer.Tars.Communication.Email.MailKit.DI;

public static class MailKitEmailServicesDI
{
    /// <summary>
    /// Registers the MailKit <see cref="IEmailSender"/>. Pair with
    /// <see cref="MailKitEmailOptionsDI.AddTarsMailKitEmailOptions"/> to supply SMTP configuration.
    /// </summary>
    public static IServiceCollection AddTarsMailKitEmailSender(this IServiceCollection services)
    {
        services.TryAddSingleton<IEmailSender, MailKitEmailSender>();
        return services;
    }
}
