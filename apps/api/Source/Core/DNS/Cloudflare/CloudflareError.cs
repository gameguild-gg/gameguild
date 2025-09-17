using System.Text.Json.Serialization;


namespace GameGuild.DNS.Cloudflare;

/// <summary>
/// Cloudflare API error.
/// </summary>
public class CloudflareError {
  [JsonPropertyName("code")] public int Code { get; set; }

  [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}
