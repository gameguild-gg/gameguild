using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Adapter that implements <see cref="IUserContext"/> on top of <see cref="ActorContext"/>.
/// </summary>
/// <remarks>
///     <para>
///         This adapter allows gradual migration from HttpContext-based UserContext
///         to the new ActorContext model. New code should use <see cref="IActorContextAccessor"/> directly.
///     </para>
/// </remarks>
[Obsolete("Use IActorContextAccessor for new code. This adapter is provided for backward compatibility.")]
public sealed class ActorBasedUserContext : IUserContext
{
    private readonly IActorContextAccessor _actorContextAccessor;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ActorBasedUserContext"/> class.
    /// </summary>
    public ActorBasedUserContext(IActorContextAccessor actorContextAccessor)
    {
        _actorContextAccessor = actorContextAccessor ?? throw new ArgumentNullException(nameof(actorContextAccessor));
    }

    private ActorContext Context => _actorContextAccessor.ActorContext;

    /// <inheritdoc />
    public Guid? UserId => Context.SubjectIdAsGuid;

    /// <inheritdoc />
    public string? Email => Context.GetAttribute("email");

    /// <inheritdoc />
    public string? Name => Context.GetAttribute("name");

    /// <inheritdoc />
    public IDictionary<string, object> Claims
    {
        get
        {
            var result = new Dictionary<string, object>();
            foreach (var (key, value) in Context.Attributes)
            {
                result[key] = value;
            }
            return result;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated => Context.IsAuthenticated;

    /// <inheritdoc />
    public IEnumerable<string> Roles => Context.Roles;

    /// <inheritdoc />
    public bool IsInRole(string role) => Context.IsInRole(role);
}
