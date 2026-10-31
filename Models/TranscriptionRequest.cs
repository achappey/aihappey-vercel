using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIHappey.Vercel.Models;

/// <summary>
/// Vercel Model Gateway v3 compatible request DTO for <c>POST /v1/audio/transcriptions</c>.
/// <para>
/// This JSON shape is a public, contract-locked surface. Do not rename properties, change casing,
/// alter types, or restructure this DTO.
/// </para>
/// </summary>
public class TranscriptionRequest
{

    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    /// <summary>
    /// Audio input as a data-url (<c>data:audio/...;base64,...</c>) or raw base64.
    /// </summary>
    [JsonPropertyName("audio")]
    public object Audio { get; set; } = null!;

    /// <summary>
    /// Media type for <see cref="Audio"/> (e.g. <c>audio/mpeg</c>). Must match the provided audio payload.
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = null!;

    [JsonPropertyName("providerOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderOptions { get; set; }

}

/// <summary>
/// Vercel Model Gateway v3 compatible response DTO for <c>POST /v1/audio/transcriptions</c>.
/// </summary>
public class TranscriptionResponse
{
    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    [JsonPropertyName("durationInSeconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? DurationInSeconds { get; set; }

    [JsonPropertyName("warnings")]
    public IEnumerable<object> Warnings { get; set; } = [];

    [JsonPropertyName("segments")]
    public IEnumerable<TranscriptionSegment> Segments { get; set; } = [];

    [JsonPropertyName("response")]
    public ResponseData Response { get; set; } = default!;

    /**
        * Optional request information for telemetry and debugging purposes.
        */
    [JsonPropertyName("request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TranscriptionRequestItem? Request { get; set; }

}

public class TranscriptionRequestItem
{
    /**
         * Raw request HTTP body that was sent to the provider API as a string (JSON should be stringified).
         * Non-HTTP(s) providers should not set this.
         */

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Body { get; set; }
}

public class TranscriptionSegment
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("startSecond")]
    public double StartSecond { get; set; }

    [JsonPropertyName("endSecond")]
    public double EndSecond { get; set; }


}
