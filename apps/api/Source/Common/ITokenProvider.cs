using GameGuild.Modules.Users.Models;


namespace GameGuild.Common.Abstractions;

public interface ITokenProvider
{
    string Create(User user);
}
