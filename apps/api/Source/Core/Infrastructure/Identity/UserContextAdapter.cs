using GameGuild.Core.Domain.Identity;
using IUserContextCommon = GameGuild.IUserContext;
using IUserContextCore = GameGuild.Core.Domain.Identity.IUserContext;

namespace GameGuild.Core.Infrastructure.Identity;

/// <summary>
/// Adapter that implements the Common IUserContext interface while delegating to Core implementation
/// Provides backward compatibility during migration from Common to Core
/// </summary>
public class UserContextAdapter : IUserContextCommon {
    private readonly IUserContextCore _coreUserContext;

    public UserContextAdapter(IUserContextCore coreUserContext) {
        _coreUserContext = coreUserContext;
    }

    public Guid? UserId => _coreUserContext.UserId;
    public string? Email => _coreUserContext.Email;
    public string? Name => _coreUserContext.Name;
    public IDictionary<string, object> Claims => _coreUserContext.Claims;
    public bool IsAuthenticated => _coreUserContext.IsAuthenticated;
    public IEnumerable<string> Roles => _coreUserContext.Roles;

    public bool IsInRole(string role) => _coreUserContext.IsInRole(role);
}
