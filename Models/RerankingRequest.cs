using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIHappey.Vercel.Models;

public class RerankingRequest
{

    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    [JsonPropertyName("documents")]
    public RerankingDocument Documents { get; set; } = null!;

    [JsonPropertyName("query")]
    public string Query { get; set; } = null!;

    [JsonPropertyName("topN")]
    public int? TopN { get; set; }

    [JsonPropertyName("providerOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderOptions { get; set; }
}

public class RerankingDocument
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("values")]
    public required JsonElement Values { get; init; }
}

public class RerankResponseData : ResponseData
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }
}

public class RerankingResponse
{
    [JsonPropertyName("ranking")]
    public IEnumerable<RerankingRanking> Ranking { get; set; } = null!;

    [JsonPropertyName("warnings")]
    public IEnumerable<object> Warnings { get; set; } = [];

    [JsonPropertyName("response")]
    public RerankResponseData Response { get; set; } = default!;

    [JsonPropertyName("providerMetadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? ProviderMetadata { get; set; }
}

public class RerankingRanking
{
    [JsonPropertyName("index")]
    public required int Index { get; init; }

    [JsonPropertyName("relevanceScore")]
    public required double RelevanceScore { get; init; }
}
