namespace GameGuild.Modules.Auth.Dtos;

/// <summary>
/// Tenant information included in authentication responses
/// </summary>
public class TenantInfoDto
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid Id
    {
        get;
        set;
    }

    /// <summary>
    /// Tenant name
    /// </summary>
    public string Name
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// Whether tenant is active
    /// </summary>
    public bool IsActive
    {
        get;
        set;
    }
}
