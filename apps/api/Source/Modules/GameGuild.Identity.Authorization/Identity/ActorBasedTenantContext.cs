using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Adapter that implements <see cref="ITenantContext"/> on top of <see cref="ActorContext"/>.
/// </summary>
/// <remarks>
///     <para>
///         This adapter allows gradual migration from HttpContext-based TenantContext
///         to the new ActorContext model. New code should use <see cref="IActorContextAccessor"/> directly.
///     </para>
/// </remarks>
[Obsolete("Use IActorContextAccessor for new code. This adapter is provided for backward compatibility.")]
public sealed class ActorBasedTenantContext : ITenantContext
{
    private readonly IActorContextAccessor _actorContextAccessor;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ActorBasedTenantContext"/> class.
    /// </summary>
    public ActorBasedTenantContext(IActorContextAccessor actorContextAccessor)
    {
        _actorContextAccessor = actorContextAccessor ?? throw new ArgumentNullException(nameof(actorContextAccessor));
    }

    private ActorContext Context => _actorContextAccessor.ActorContext;

    /// <inheritdoc />
    public Guid? TenantId => Context.TenantId;

    /// <inheritdoc />
    public string? TenantName => Context.GetAttribute("tenant_name");

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool IsActive
    {
        get
        {
            var isActive = Context.GetAttribute("tenant_active");
            return bool.TryParse(isActive, out var active) && active;
        }
    }

    /// <inheritdoc />
    public string? SubscriptionPlan => Context.GetAttribute("subscription_plan");
}
