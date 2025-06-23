using MediatR;

namespace GameGuild.Modules.Auth.Commands;

/// <summary>
/// Command to update user profile with validation
/// </summary>
public class UpdateUserProfileCommand : IRequest<User.Models.User>
{
    private Guid _userId;

    private string? _name;

    private string? _email;

    private bool? _isActive;

    public Guid UserId
    {
        get => _userId;
        set => _userId = value;
    }

    public string? Name
    {
        get => _name;
        set => _name = value;
    }

    public string? Email
    {
        get => _email;
        set => _email = value;
    }

    public bool? IsActive
    {
        get => _isActive;
        set => _isActive = value;
    }
}
