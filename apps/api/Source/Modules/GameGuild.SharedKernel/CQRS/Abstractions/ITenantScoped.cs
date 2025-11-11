namespace GameGuild.Abstractions;

public interface ITenantScoped
{
    Guid? TenantId { get; }
}
