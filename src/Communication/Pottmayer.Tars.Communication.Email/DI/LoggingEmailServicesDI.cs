using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Communication.Email.Abstractions;

namespace Pottmayer.Tars.Communication.Email.DI;

public static class LoggingEmailServicesDI
{
    /// <summary>
    /// Registers the logging (fake) <see cref="IEmailSender"/>. Intended for dev/test, where e-mail is
    /// written to the logs instead of being delivered.
    /// </summary>
    public static IServiceCollection AddTarsLoggingEmailSender(this IServiceCollection services)
    {
        services.TryAddSingleton<IEmailSender, LoggingEmailSender>();
        return services;
    }
}
