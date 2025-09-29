namespace GameGuild.Source.Core.Tenants;

/// <summary>
/// Data transfer object for permission definitions
/// </summary>
public class PermissionDefinition {
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Scope { get; set; }
    public Dictionary<string, object>? Conditions { get; set; }
}