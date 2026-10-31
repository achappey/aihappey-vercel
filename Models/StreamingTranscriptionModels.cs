using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIHappey.Vercel.Models;

/// <summary>AI SDK TranscriptionModelV4 streaming request.</summary>
public sealed class StreamingTranscriptionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    /// <summary>The complete audio file encoded as plain base64.</summary>
    [JsonPropertyName("audio")]
    public string Audio { get; set; } = null!;

    [JsonPropertyName("inputAudioFormat")]
    public AudioFormat InputAudioFormat { get; set; } = null!;

    [JsonPropertyName("providerOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderOptions { get; set; }

    [JsonPropertyName("includeRawChunks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeRawChunks { get; set; }
}

public sealed class AudioFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("rate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Rate { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TranscriptionStreamStartPart), "stream-start")]
[JsonDerivedType(typeof(TranscriptionDeltaPart), "transcript-delta")]
[JsonDerivedType(typeof(TranscriptionPartialPart), "transcript-partial")]
[JsonDerivedType(typeof(TranscriptionFinalPart), "transcript-final")]
[JsonDerivedType(typeof(TranscriptionResponseMetadataPart), "response-metadata")]
[JsonDerivedType(typeof(TranscriptionFinishPart), "finish")]
[JsonDerivedType(typeof(TranscriptionRawPart), "raw")]
[JsonDerivedType(typeof(TranscriptionErrorPart), "error")]
public abstract class StreamingTranscriptionPart;

public sealed class TranscriptionStreamStartPart : StreamingTranscriptionPart
{
    [JsonPropertyName("warnings")]
    public IEnumerable<object> Warnings { get; set; } = [];
}

public sealed class TranscriptionDeltaPart : StreamingTranscriptionPart
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("delta")]
    public string Delta { get; set; } = null!;

    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }
}

public sealed class TranscriptionPartialPart : StreamingTranscriptionPart
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("startSecond")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? StartSecond { get; set; }

    [JsonPropertyName("durationInSeconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? DurationInSeconds { get; set; }

    [JsonPropertyName("channelIndex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ChannelIndex { get; set; }

    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }
}

public sealed class TranscriptionFinalPart : StreamingTranscriptionPart
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("startSecond")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? StartSecond { get; set; }

    [JsonPropertyName("endSecond")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? EndSecond { get; set; }

    [JsonPropertyName("channelIndex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ChannelIndex { get; set; }

    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }
}

public sealed class TranscriptionResponseMetadataPart : StreamingTranscriptionPart
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

public sealed class TranscriptionFinishPart : StreamingTranscriptionPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("segments")]
    public IEnumerable<TranscriptionSegment> Segments { get; set; } = [];

    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    [JsonPropertyName("durationInSeconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? DurationInSeconds { get; set; }

    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? Usage { get; set; }

    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }
}

public sealed class TranscriptionRawPart : StreamingTranscriptionPart
{
    [JsonPropertyName("rawValue")]
    public object? RawValue { get; set; }
}

public sealed class TranscriptionErrorPart : StreamingTranscriptionPart
{
    [JsonPropertyName("error")]
    public object? Error { get; set; }
}
