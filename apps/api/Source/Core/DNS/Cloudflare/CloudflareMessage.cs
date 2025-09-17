using System.Text.Json.Serialization;


namespace GameGuild.DNS.Cloudfare;

/// <summary>
/// Cloudflare API message.
/// </summary>
public class CloudflareMessage {
  [JsonPropertyName("code")] public int Code { get; set; }

  [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}
