namespace GameGuild.Identity.Context.Actors.Adapters;

/// <summary>
///     Adapter that implements the legacy IUserContext interface on top of ActorContext.
/// </summary>
/// <remarks>
///     <para>
///         This adapter enables gradual migration from IUserContext to ActorContext.
///         New code should use IActorContextAccessor directly.
///     </para>
///     <para>
///         NOTE: This interface mirrors GameGuild.Identity.Authorization.IUserContext.
///         Import this file in Authorization module and register the adapter.
///     </para>
/// </remarks>
public sealed class ActorContextUserAdapter
{
    private readonly IActorContextAccessor _actorContextAccessor;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ActorContextUserAdapter"/> class.
    /// </summary>
    public ActorContextUserAdapter(IActorContextAccessor actorContextAccessor)
    {
        _actorContextAccessor = actorContextAccessor ?? throw new ArgumentNullException(nameof(actorContextAccessor));
    }

    private ActorContext Context => _actorContextAccessor.ActorContext;

    /// <summary>
    ///     Gets the current user's ID.
    /// </summary>
    public Guid? UserId => Context.SubjectIdAsGuid;

    /// <summary>
    ///     Gets the current user's email address.
    /// </summary>
    public string? Email => Context.GetAttribute("email");

    /// <summary>
    ///     Gets the current user's display name.
    /// </summary>
    public string? Name => Context.GetAttribute("name");

    /// <summary>
    ///     Gets all claims for the current user as a dictionary.
    /// </summary>
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

    /// <summary>
    ///     Gets whether the current user is authenticated.
    /// </summary>
    public bool IsAuthenticated => Context.IsAuthenticated;

    /// <summary>
    ///     Gets all roles for the current user.
    /// </summary>
    public IEnumerable<string> Roles => Context.Roles;

    /// <summary>
    ///     Checks if the current user is in a specific role.
    /// </summary>
    public bool IsInRole(string role) => Context.IsInRole(role);
}
