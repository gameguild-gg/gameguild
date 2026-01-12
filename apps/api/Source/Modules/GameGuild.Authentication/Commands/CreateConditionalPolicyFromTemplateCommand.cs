using GameGuild.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Authentication;

public record CreateConditionalPolicyFromTemplateCommand : ICommand<ConditionalPolicy>
{
    public Guid TemplateId { get; set; }

    public string Name { get; init; } = string.Empty;

    public Guid TenantId { get; init; }

    public Dictionary<string, object> TemplateParameters { get; init; } = new Dictionary<string, object>();
}
