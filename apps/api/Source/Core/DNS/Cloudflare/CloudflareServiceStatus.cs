namespace GameGuild.Controllers;

/// <summary> Status information for the Cloudflare Dynamic DNS service. </summary>
public class CloudflareServiceStatus
{
    public bool IsRunning { get; set; }

    public string? LastKnownIp { get; set; }

    public DateTime? LastUpdate { get; set; }
}