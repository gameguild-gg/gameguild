using GameGuild.Authentication.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record ApplyPermissionTemplateCommand : ICommand<ApplyPermissionTemplateResult>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public Guid TemplateId { get; init; }

    public string? AppliedBy { get; init; }

    public string? Reason { get; init; }
}
