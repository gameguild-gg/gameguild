namespace GameGuild.Authentication.DTOs;

public abstract class ApplyPermissionTemplateResult
{
    public Guid UserId { get; set; }

    public Guid TemplateId { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public int PermissionsGranted { get; set; }

    public List<string> Errors { get; set; } = new List<string>();

    public DateTime AppliedAt { get; set; }
}
