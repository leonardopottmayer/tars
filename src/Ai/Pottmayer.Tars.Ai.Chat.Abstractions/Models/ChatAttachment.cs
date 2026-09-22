namespace Pottmayer.Tars.Ai.Chat.Abstractions.Models;

/// <summary>
/// Binary content sent inline with a user turn — a voice note, a photo — for providers that accept
/// multimodal input. <see cref="MimeType"/> tells the provider how to read <see cref="Data"/>
/// (e.g. <c>audio/ogg</c>, <c>image/jpeg</c>).
/// </summary>
public sealed record ChatAttachment(ReadOnlyMemory<byte> Data, string MimeType);
