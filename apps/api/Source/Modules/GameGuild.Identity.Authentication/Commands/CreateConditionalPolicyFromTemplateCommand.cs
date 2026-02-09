using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record CreateConditionalPolicyFromTemplateCommand : ICommand<ConditionalPolicy>
{
    public Guid TemplateId { get; set; }

    public string Name { get; init; } = string.Empty;

    public Guid TenantId { get; init; }

    public Dictionary<string, object> TemplateParameters { get; init; } = new Dictionary<string, object>();
}
