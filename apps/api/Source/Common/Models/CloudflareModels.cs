using System.Text.Json.Serialization;

namespace GameGuild.Common.Models.Cloudflare;

/// <summary>
/// Cloudflare API response wrapper.
/// </summary>
/// <typeparam name="T">The response data type</typeparam>
public class CloudflareApiResponse<T> {
  [JsonPropertyName("success")]
  public bool Success { get; set; }

  [JsonPropertyName("errors")]
  public List<CloudflareError> Errors { get; set; } = new();

  [JsonPropertyName("messages")]
  public List<CloudflareMessage> Messages { get; set; } = new();

  [JsonPropertyName("result")]
  public T? Result { get; set; }

  [JsonPropertyName("result_info")]
  public CloudflareResultInfo? ResultInfo { get; set; }
}

/// <summary>
/// Cloudflare API error.
/// </summary>
public class CloudflareError {
  [JsonPropertyName("code")]
  public int Code { get; set; }

  [JsonPropertyName("message")]
  public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Cloudflare API message.
/// </summary>
public class CloudflareMessage {
  [JsonPropertyName("code")]
  public int Code { get; set; }

  [JsonPropertyName("message")]
  public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Cloudflare API result info for pagination.
/// </summary>
public class CloudflareResultInfo {
  [JsonPropertyName("page")]
  public int Page { get; set; }

  [JsonPropertyName("per_page")]
  public int PerPage { get; set; }

  [JsonPropertyName("count")]
  public int Count { get; set; }

  [JsonPropertyName("total_count")]
  public int TotalCount { get; set; }
}

/// <summary>
/// Cloudflare DNS record.
/// </summary>
public class CloudflareDnsRecord {
  [JsonPropertyName("id")]
  public string Id { get; set; } = string.Empty;

  [JsonPropertyName("zone_id")]
  public string ZoneId { get; set; } = string.Empty;

  [JsonPropertyName("zone_name")]
  public string ZoneName { get; set; } = string.Empty;

  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("type")]
  public string Type { get; set; } = string.Empty;

  [JsonPropertyName("content")]
  public string Content { get; set; } = string.Empty;

  [JsonPropertyName("proxiable")]
  public bool Proxiable { get; set; }

  [JsonPropertyName("proxied")]
  public bool Proxied { get; set; }

  [JsonPropertyName("ttl")]
  public int Ttl { get; set; }

  [JsonPropertyName("locked")]
  public bool Locked { get; set; }

  [JsonPropertyName("meta")]
  public CloudflareDnsRecordMeta Meta { get; set; } = new();

  [JsonPropertyName("comment")]
  public string? Comment { get; set; }

  [JsonPropertyName("tags")]
  public List<string> Tags { get; set; } = new();

  [JsonPropertyName("created_on")]
  public DateTime CreatedOn { get; set; }

  [JsonPropertyName("modified_on")]
  public DateTime ModifiedOn { get; set; }
}

/// <summary>
/// Cloudflare DNS record metadata.
/// </summary>
public class CloudflareDnsRecordMeta {
  [JsonPropertyName("auto_added")]
  public bool AutoAdded { get; set; }

  [JsonPropertyName("managed_by_apps")]
  public bool ManagedByApps { get; set; }

  [JsonPropertyName("managed_by_argo_tunnel")]
  public bool ManagedByArgoTunnel { get; set; }
}

/// <summary>
/// Request to create or update a Cloudflare DNS record.
/// </summary>
public class CloudflareDnsRecordRequest {
  [JsonPropertyName("type")]
  public string Type { get; set; } = string.Empty;

  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("content")]
  public string Content { get; set; } = string.Empty;

  [JsonPropertyName("ttl")]
  public int Ttl { get; set; } = 300;

  [JsonPropertyName("proxied")]
  public bool Proxied { get; set; } = false;

  [JsonPropertyName("comment")]
  public string? Comment { get; set; }

  [JsonPropertyName("tags")]
  public List<string> Tags { get; set; } = new();
}
