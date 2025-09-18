using IUserContextCommon = GameGuild.IUserContext;
using IUserContextCore = GameGuild.Core.Domain.Identity.IUserContext;


namespace GameGuild.Authorization.Identity;

/// <summary> Adapter that implements the Common IUserContext interface while delegating to Core implementation Provides backward compatibility during migration from Common to Core </summary>
public class UserContextAdapter : IUserContextCommon {
  private readonly IUserContextCore _coreUserContext;

  public UserContextAdapter(IUserContextCore coreUserContext) { _coreUserContext = coreUserContext; }

  public Guid? UserId { get => _coreUserContext.UserId; }

  public string? Email { get => _coreUserContext.Email; }

  public string? Name { get => _coreUserContext.Name; }

  public IDictionary<string, object> Claims { get => _coreUserContext.Claims; }

  public bool IsAuthenticated { get => _coreUserContext.IsAuthenticated; }

  public IEnumerable<string> Roles { get => _coreUserContext.Roles; }

  public bool IsInRole(string role) { return _coreUserContext.IsInRole(role); }
}
