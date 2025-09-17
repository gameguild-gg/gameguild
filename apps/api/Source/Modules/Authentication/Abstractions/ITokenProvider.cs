using GameGuild.Modules.Users;


namespace GameGuild.Modules.Authentication;

public interface ITokenProvider {
  string Create(IUser user);
}
