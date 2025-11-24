namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Tenant information
/// </summary>
public abstract class TenantInfo
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string[ ] Roles { get; set; } = [];
}
