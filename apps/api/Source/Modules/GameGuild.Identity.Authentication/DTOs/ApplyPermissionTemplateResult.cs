namespace GameGuild.Identity.Authentication;

public class ApplyPermissionTemplateResult
{
    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }

    public Guid TemplateId { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public int PermissionsGranted { get; set; }

    public List<string> GrantedPermissions { get; set; } = new List<string>();

    public List<string> Errors { get; set; } = new List<string>();

    public bool Success { get; set; }

    public DateTime AppliedAt { get; set; }

    public string? AppliedBy { get; set; }

    public static ApplyPermissionTemplateResult SuccessResult(
        Guid userId,
        Guid? tenantId,
        Guid templateId,
        string templateName,
        List<string> permissions,
        string? appliedBy)
    {
        return new ApplyPermissionTemplateResult
        {
            UserId = userId,
            TenantId = tenantId,
            TemplateId = templateId,
            TemplateName = templateName,
            PermissionsGranted = permissions.Count,
            GrantedPermissions = permissions,
            Success = true,
            AppliedAt = SystemClock.UtcNow,
            AppliedBy = appliedBy
        };
    }

    public static ApplyPermissionTemplateResult Failure(Guid userId, Guid templateId, string error)
    {
        return new ApplyPermissionTemplateResult
        {
            UserId = userId,
            TemplateId = templateId,
            Success = false,
            Errors = new List<string> { error },
            AppliedAt = SystemClock.UtcNow
        };
    }
}
