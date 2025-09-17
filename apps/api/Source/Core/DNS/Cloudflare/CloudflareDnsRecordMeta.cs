using System.Text.Json.Serialization;


namespace GameGuild.DNS.Cloudfare;

/// <summary>
/// Cloudflare DNS record metadata.
/// </summary>
public class CloudflareDnsRecordMeta {
  [JsonPropertyName("auto_added")] public bool AutoAdded { get; set; }

  [JsonPropertyName("managed_by_apps")] public bool ManagedByApps { get; set; }

  [JsonPropertyName("managed_by_argo_tunnel")] public bool ManagedByArgoTunnel { get; set; }
}
