namespace GameGuild.Resources.Commands;

/// <summary>
///     Request DTO for toggling a resource quota
/// </summary>
public class ToggleResourceQuotaRequest
{
    public bool IsActive { get; set; }
}
