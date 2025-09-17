using System.Text.Json.Serialization;


namespace GameGuild.DNS.Cloudfare;

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
