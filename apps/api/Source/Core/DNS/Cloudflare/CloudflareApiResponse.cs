using System.Text.Json.Serialization;


namespace GameGuild.DNS.Cloudflare;

/// <summary>
/// Cloudflare API response model
/// </summary>
public class CloudflareApiResponse {
  public bool Success { get; set; }

  public List<string>? Errors { get; set; }

  public List<string>? Messages { get; set; }
}

/// <summary>
/// Cloudflare API response wrapper.
/// </summary>
/// <typeparam name="T">The response data type</typeparam>
public class CloudflareApiResponse<T> {
  [JsonPropertyName("success")] public bool Success { get; set; }

  [JsonPropertyName("errors")] public List<CloudflareError> Errors { get; set; } = new();

  [JsonPropertyName("messages")] public List<CloudflareMessage> Messages { get; set; } = new();

  [JsonPropertyName("result")] public T? Result { get; set; }

  [JsonPropertyName("result_info")] public CloudflareResultInfo? ResultInfo { get; set; }
}
