using GameGuild.Core.Domain.Identity;
using ITenantContextCommon = GameGuild.ITenantContext;
using ITenantContextCore = GameGuild.Core.Domain.Identity.ITenantContext;

namespace GameGuild.Core.Infrastructure.Identity;

/// <summary>
/// Adapter that implements the Common ITenantContext interface while delegating to Core implementation
/// Provides backward compatibility during migration from Common to Core
/// </summary>
public class TenantContextAdapter : ITenantContextCommon {
    private readonly ITenantContextCore _coreTenantContext;

    public TenantContextAdapter(ITenantContextCore coreTenantContext) {
        _coreTenantContext = coreTenantContext;
    }

    public Guid? TenantId => _coreTenantContext.TenantId;
    public string? TenantName => _coreTenantContext.TenantName;
    public IDictionary<string, object> Settings => _coreTenantContext.Settings;
    public bool IsActive => _coreTenantContext.IsActive;
    public string? SubscriptionPlan => _coreTenantContext.SubscriptionPlan;
}
