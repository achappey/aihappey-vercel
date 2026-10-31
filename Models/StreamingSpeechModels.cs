using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIHappey.Vercel.Models;

/// <summary>
/// AI SDK SpeechModelV4 streaming request for POST /api/speech/stream.
/// The text input is buffered as an ordered JSON array, not streamed over the wire.
/// </summary>
public sealed class StreamingSpeechRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    /// <summary>Consecutive parts form one script; boundaries may occur mid-word.</summary>
    [JsonPropertyName("text")]
    public StreamingSpeechTextPart[] Text { get; set; } = null!;

    [JsonPropertyName("voice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Voice { get; set; }

    [JsonPropertyName("outputFormat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AudioFormat? OutputFormat { get; set; }

    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; set; }

    [JsonPropertyName("speed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Speed { get; set; }

    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    [JsonPropertyName("providerOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderOptions { get; set; }

    [JsonPropertyName("includeRawChunks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeRawChunks { get; set; }
}

public sealed class StreamingSpeechTextPart
{
    [Required]
    [RegularExpression("^text$")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [Required(AllowEmptyStrings = true)]
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    /// <summary>Defaults to the request's voice, or the provider's default voice.</summary>
    [JsonPropertyName("voice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Voice { get; set; }

    [JsonPropertyName("providerOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderOptions { get; set; }
}

public sealed class StreamingSpeechUsage
{
    /// <summary>Number of input Unicode code points, if reported by the provider.</summary>
    [JsonPropertyName("inputCharacters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? InputCharacters { get; set; }

    [JsonPropertyName("inputTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? InputTokens { get; set; }

    [JsonPropertyName("outputTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? OutputTokens { get; set; }

    [JsonPropertyName("raw")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? Raw { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SpeechStreamStartPart), "stream-start")]
[JsonDerivedType(typeof(SpeechAudioPart), "audio")]
[JsonDerivedType(typeof(SpeechResponseMetadataPart), "response-metadata")]
[JsonDerivedType(typeof(SpeechFinishPart), "finish")]
[JsonDerivedType(typeof(SpeechRawPart), "raw")]
[JsonDerivedType(typeof(SpeechErrorPart), "error")]
public abstract class StreamingSpeechPart;

public sealed class SpeechStreamStartPart : StreamingSpeechPart
{
    [JsonPropertyName("warnings")]
    public IEnumerable<object> Warnings { get; set; } = [];
}

public sealed class SpeechAudioPart : StreamingSpeechPart
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    /// <summary>
    /// Plain base64-encoded audio bytes, not a data URL. Chunks are ordered and
    /// need not be standalone files. Empty chunks may carry provider metadata.
    /// </summary>
    [JsonPropertyName("audio")]
    public string Audio { get; set; } = null!;

    [JsonPropertyName("audioFormat")]
    public AudioFormat AudioFormat { get; set; } = null!;

    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }
}

public sealed class SpeechResponseMetadataPart : StreamingSpeechPart
{
    [JsonPropertyName("timestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("modelId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ModelId { get; set; }

    [JsonPropertyName("headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Body { get; set; }
}

/// <summary>Emitted exactly once after all audio chunks on successful completion.</summary>
public sealed class SpeechFinishPart : StreamingSpeechPart
{
    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StreamingSpeechUsage? Usage { get; set; }

    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }
}

public sealed class SpeechRawPart : StreamingSpeechPart
{
    [JsonPropertyName("rawValue")]
    public object? RawValue { get; set; }
}

/// <summary>
/// A non-fatal provider error may be followed by more events. An unrecoverable
/// failure closes the stream without a finish part.
/// </summary>
public sealed class SpeechErrorPart : StreamingSpeechPart
{
    [JsonPropertyName("error")]
    public object? Error { get; set; }
}
