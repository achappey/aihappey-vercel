using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIHappey.Vercel.Models;

public class VideoRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = null!;

    [JsonPropertyName("resolution")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Resolution { get; set; }

    [JsonPropertyName("aspectRatio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AspectRatio { get; set; }

    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; set; }

    [JsonPropertyName("duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Duration { get; set; }

    [JsonPropertyName("fps")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Fps { get; set; }

    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? N { get; set; }

    [JsonPropertyName("providerOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderOptions { get; set; }

    [JsonPropertyName("image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VideoFile? Image { get; set; }

    [JsonPropertyName("inputReferences")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<VideoFile>? InputReferences { get; set; }

    [JsonPropertyName("frameImages")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<VideoFrameImage>? FrameImages { get; set; }

    [JsonPropertyName("generateAudio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? GenerateAudio { get; set; }

}

public class VideoFrameImage
{
    [JsonPropertyName("frameType")]
    public string FrameType { get; set; } = null!;  // 'first_frame' or 'last_frame'

    [JsonPropertyName("image")]
    public VideoFile Image { get; set; } = null!;
}

[JsonConverter(typeof(VideoFileJsonConverter))]
public class VideoFile : IValidatableObject
{
    public string Type { get; set; } = "file";

    public string? MediaType { get; set; }

    public string? Data { get; set; }

    [JsonPropertyName("providerOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderOptions { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => MediaFileJson.Validate(Type, this is VideoFileUrl url ? url.Url : null, MediaType, Data);
}

[JsonConverter(typeof(VideoFileJsonConverter))]
public class VideoFileUrl : VideoFile
{
    public VideoFileUrl() => Type = "url";

    public string Url { get; set; } = null!;
}

public class VideoResponse
{
    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }

    [JsonPropertyName("videos")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<VideoResponseFile>? Videos { get; set; }

    [JsonPropertyName("warnings")]
    public IEnumerable<object> Warnings { get; set; } = [];

    [JsonPropertyName("response")]
    public HeaderResponseData Response { get; set; } = default!;
}

public class VideoResponseFile
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "base64";

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Data { get; set; } = null!;

    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = null!;
}

/// <summary>
/// Result returned when a provider starts an asynchronous video operation.
/// The operation is provider-local until the API controller prefixes it with
/// the provider identifier for transport to the client.
/// </summary>
public sealed class VideoOperationStartResult
{
    [JsonPropertyName("operation")]
    public string Operation { get; set; } = null!;

    [JsonPropertyName("warnings")]
    public IEnumerable<object> Warnings { get; set; } = [];

    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }

    [JsonPropertyName("response")]
    public HeaderResponseData Response { get; set; } = default!;
}

/// <summary>
/// Common response data returned while checking an asynchronous video operation.
/// Concrete pending, completed, and error types preserve the Vercel V4
/// discriminated-union JSON shape.
/// </summary>
[JsonConverter(typeof(VideoOperationStatusResultJsonConverter))]
public abstract class VideoOperationStatusResult
{
    [JsonPropertyName("status")]
    public abstract string Status { get; }

    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }

    [JsonPropertyName("response")]
    public HeaderResponseData Response { get; set; } = default!;
}

public sealed class VideoOperationPendingResult : VideoOperationStatusResult
{
    public override string Status => "pending";

    [JsonPropertyName("warnings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<object>? Warnings { get; set; }
}

public sealed class VideoOperationCompletedResult : VideoOperationStatusResult
{
    public override string Status => "completed";

    [JsonPropertyName("videos")]
    public IEnumerable<VideoOperationVideoData> Videos { get; set; } = [];

    [JsonPropertyName("warnings")]
    public IEnumerable<object> Warnings { get; set; } = [];
}

public sealed class VideoOperationErrorResult : VideoOperationStatusResult
{
    public override string Status => "error";

    [JsonPropertyName("error")]
    public string Error { get; set; } = null!;
}

/// <summary>
/// Vercel VideoModelV4 video data.
/// base64 and binary results use <see cref="Data"/>. Binary values may be
/// represented by a byte array and are serialized by System.Text.Json.
/// </summary>
public sealed class VideoOperationVideoData
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = null!;
}

public sealed class VideoOperationStatusResultJsonConverter : JsonConverter<VideoOperationStatusResult>
{
    public override VideoOperationStatusResult Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("status", out var status)
            || status.ValueKind != JsonValueKind.String)
            throw new JsonException("A video operation status requires a string 'status' discriminator.");

        return status.GetString() switch
        {
            "pending" => root.Deserialize<VideoOperationPendingResult>(options)
                ?? throw new JsonException("Video operation status cannot be null."),
            "completed" => root.Deserialize<VideoOperationCompletedResult>(options)
                ?? throw new JsonException("Video operation status cannot be null."),
            "error" => root.Deserialize<VideoOperationErrorResult>(options)
                ?? throw new JsonException("Video operation status cannot be null."),
            var value => throw new JsonException($"Unsupported video operation status '{value}'.")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        VideoOperationStatusResult value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("status", value.Status);
        Write("providerMetadata", value.ProviderMetadata);
        Write("response", value.Response);

        switch (value)
        {
            case VideoOperationPendingResult pending:
                Write("warnings", pending.Warnings);
                break;
            case VideoOperationCompletedResult completed:
                Write("videos", completed.Videos);
                Write("warnings", completed.Warnings);
                break;
            case VideoOperationErrorResult error:
                Write("error", error.Error);
                break;
            default:
                throw new JsonException($"Unsupported video operation result type '{value.GetType()}'.");
        }

        writer.WriteEndObject();

        void Write<T>(string name, T? property)
        {
            if (property is null) return;
            writer.WritePropertyName(name);
            JsonSerializer.Serialize(writer, property, options);
        }
    }
}
