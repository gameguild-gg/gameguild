
namespace GameGuild.Identity.Authentication;

/// <summary>
///     Composite authentication service interface for backward compatibility — inherits all auth sub-service interfaces
/// </summary>
public interface IAuthService : ILocalAuthService, IOAuthAuthService, IPasswordService, IWeb3AuthService
{
}
