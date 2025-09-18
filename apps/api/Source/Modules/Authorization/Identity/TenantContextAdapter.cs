using ITenantContextCommon = GameGuild.ITenantContext;
using ITenantContextCore = GameGuild.Core.Domain.Identity.ITenantContext;


namespace GameGuild.Authorization.Identity;

/// <summary> Adapter that implements the Common ITenantContext interface while delegating to Core implementation Provides backward compatibility during migration from Common to Core </summary>
public class TenantContextAdapter : ITenantContextCommon {
  private readonly ITenantContextCore _coreTenantContext;

  public TenantContextAdapter(ITenantContextCore coreTenantContext) { _coreTenantContext = coreTenantContext; }

  public Guid? TenantId { get => _coreTenantContext.TenantId; }

  public string? TenantName { get => _coreTenantContext.TenantName; }

  public IDictionary<string, object> Settings { get => _coreTenantContext.Settings; }

  public bool IsActive { get => _coreTenantContext.IsActive; }

  public string? SubscriptionPlan { get => _coreTenantContext.SubscriptionPlan; }
}
