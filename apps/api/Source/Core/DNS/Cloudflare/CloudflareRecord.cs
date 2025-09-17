namespace GameGuild.DNS.Cloudflare;

/// <summary>
/// Cloudflare DNS record model
/// </summary>
public class CloudflareRecord {
  public string Id { get; set; } = string.Empty;

  public string Name { get; set; } = string.Empty;

  public string Content { get; set; } = string.Empty;
}