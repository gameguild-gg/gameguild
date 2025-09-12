namespace GameGuild;

public interface ITenantScoped {
  Guid? TenantId { get; }
}
