using System.Text.Json.Serialization;


namespace GameGuild.DNS.Cloudfare;

/// <summary>
/// Request to create or update a Cloudflare DNS record.
/// </summary>
public class CloudflareDnsRecordRequest {
  [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

  [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

  [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;

  [JsonPropertyName("ttl")] public int Ttl { get; set; } = 300;

  [JsonPropertyName("proxied")] public bool Proxied { get; set; } = false;

  [JsonPropertyName("comment")] public string? Comment { get; set; }

  [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
}
