namespace GameGuild.Identity.Context.Actors.Adapters;

/// <summary>
///     Adapter that implements the legacy ITenantContext interface on top of ActorContext.
/// </summary>
/// <remarks>
///     <para>
///         This adapter enables gradual migration from ITenantContext to ActorContext.
///         New code should use IActorContextAccessor directly.
///     </para>
///     <para>
///         NOTE: This interface mirrors GameGuild.Identity.Authorization.ITenantContext.
///         Import this file in Authorization module and register the adapter.
///     </para>
/// </remarks>
public sealed class ActorContextTenantAdapter
{
    private readonly IActorContextAccessor _actorContextAccessor;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ActorContextTenantAdapter"/> class.
    /// </summary>
    public ActorContextTenantAdapter(IActorContextAccessor actorContextAccessor)
    {
        _actorContextAccessor = actorContextAccessor ?? throw new ArgumentNullException(nameof(actorContextAccessor));
    }

    private ActorContext Context => _actorContextAccessor.ActorContext;

    /// <summary>
    ///     Gets the current tenant ID.
    /// </summary>
    public Guid? TenantId => Context.TenantId;

    /// <summary>
    ///     Gets the current tenant name.
    /// </summary>
    public string? TenantName => Context.GetAttribute("tenant_name");

    /// <summary>
    ///     Gets tenant-specific settings as a dictionary.
    /// </summary>
    public IDictionary<string, object> Settings
    {
        get
        {
            var result = new Dictionary<string, object>();
            foreach (var (key, value) in Context.Attributes)
            {
                if (key.StartsWith("tenant_setting:", StringComparison.Ordinal))
                {
                    var settingKey = key.Replace("tenant_setting:", "", StringComparison.Ordinal);
                    result[settingKey] = value;
                }
            }
            return result;
        }
    }

    /// <summary>
    ///     Gets whether the current tenant is active.
    /// </summary>
    public bool IsActive
    {
        get
        {
            var isActive = Context.GetAttribute("tenant_active");
            return bool.TryParse(isActive, out var active) && active;
        }
    }

    /// <summary>
    ///     Gets the subscription plan for the current tenant.
    /// </summary>
    public string? SubscriptionPlan => Context.GetAttribute("subscription_plan");
}
