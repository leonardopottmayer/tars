namespace Pottmayer.Tars.Ai.Chat.Gemini.Options;

/// <summary>
/// Validation entry point for <see cref="GeminiAiOptions"/>, wired into the options pipeline by
/// <c>AddTarsAiChatGeminiOptions</c> and run on application start.
/// </summary>
internal static class GeminiAiOptionsValidation
{
    /// <summary>
    /// Validates the bound <see cref="GeminiAiOptions"/> instance.
    /// </summary>
    /// <param name="options">The options instance to validate.</param>
    /// <returns><c>true</c> when non-null and <see cref="GeminiAiOptions.IsValid"/>; otherwise <c>false</c>.</returns>
    public static bool Validate(GeminiAiOptions options)
        => options is not null && options.IsValid();
}
