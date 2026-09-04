using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Ai.Chat.Gemini.Options;

namespace Pottmayer.Tars.Ai.Chat.Gemini.DI;

/// <summary>
/// Registration helper that binds <see cref="GeminiAiOptions"/> from configuration.
/// </summary>
public static class GeminiAiOptionsDI
{
    /// <summary>
    /// Binds <see cref="GeminiAiOptions"/> from configuration (default section
    /// <see cref="GeminiAiOptions.SectionName"/>, i.e. <c>Tars:Ai:Chat:Gemini</c>) and validates it on
    /// application start.
    /// </summary>
    /// <param name="builder">The host application builder whose configuration and services are used.</param>
    /// <param name="sectionName">Configuration section to bind. Defaults to <see cref="GeminiAiOptions.SectionName"/>.</param>
    /// <param name="configure">Optional code-based overrides applied after binding.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
    public static OptionsBuilder<GeminiAiOptions> AddTarsAiChatGeminiOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<GeminiAiOptions>? configure = null)
    {
        sectionName ??= GeminiAiOptions.SectionName;

        var section = builder.Configuration.GetSection(sectionName);

        var ob = builder.Services
            .AddOptions<GeminiAiOptions>()
            .Bind(section)
            .Validate(GeminiAiOptionsValidation.Validate, GeminiAiOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
