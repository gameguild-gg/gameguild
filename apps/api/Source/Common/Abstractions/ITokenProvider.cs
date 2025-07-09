namespace GameGuild.Common;

public interface ITokenProvider
{
    string Create(User user);
}
