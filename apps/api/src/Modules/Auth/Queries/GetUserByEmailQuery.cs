using MediatR;

namespace GameGuild.Modules.Auth.Queries;

/// <summary>
/// Query to get user by email
/// </summary>
public class GetUserByEmailQuery : IRequest<User.Models.User?>
{
    private string _email = string.Empty;

    public string Email
    {
        get => _email;
        set => _email = value;
    }
}
