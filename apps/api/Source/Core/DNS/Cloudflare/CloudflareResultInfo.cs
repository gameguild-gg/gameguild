using System.Text.Json.Serialization;


namespace GameGuild.DNS.Cloudflare;

/// <summary>
/// Cloudflare API result info for pagination.
/// </summary>
public class CloudflareResultInfo {
  [JsonPropertyName("page")] public int Page { get; set; }

  [JsonPropertyName("per_page")] public int PerPage { get; set; }

  [JsonPropertyName("count")] public int Count { get; set; }

  [JsonPropertyName("total_count")] public int TotalCount { get; set; }
}
