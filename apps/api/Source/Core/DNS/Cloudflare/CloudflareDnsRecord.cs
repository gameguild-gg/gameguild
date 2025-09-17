using System.Text.Json.Serialization;


namespace GameGuild.DNS.Cloudfare;

/// <summary>
/// Cloudflare DNS record.
/// </summary>
public class CloudflareDnsRecord {
  [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

  [JsonPropertyName("zone_id")] public string ZoneId { get; set; } = string.Empty;

  [JsonPropertyName("zone_name")] public string ZoneName { get; set; } = string.Empty;

  [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

  [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

  [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;

  [JsonPropertyName("proxiable")] public bool Proxiable { get; set; }

  [JsonPropertyName("proxied")] public bool Proxied { get; set; }

  [JsonPropertyName("ttl")] public int Ttl { get; set; }

  [JsonPropertyName("locked")] public bool Locked { get; set; }

  [JsonPropertyName("meta")] public CloudflareDnsRecordMeta Meta { get; set; } = new();

  [JsonPropertyName("comment")] public string? Comment { get; set; }

  [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();

  [JsonPropertyName("created_on")] public DateTime CreatedOn { get; set; }

  [JsonPropertyName("modified_on")] public DateTime ModifiedOn { get; set; }
}
