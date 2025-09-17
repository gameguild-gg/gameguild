namespace GameGuild.DNS.Cloudflare;

/// <summary>
/// Cloudflare list response model
/// </summary>
public class CloudflareListResponse {
  public bool Success { get; set; }

  public List<CloudflareRecord>? Result { get; set; }
}