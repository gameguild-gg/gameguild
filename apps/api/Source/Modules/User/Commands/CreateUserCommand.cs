using MediatR;


namespace GameGuild.Modules.User.Commands;

/// <summary>
/// Command to create a new user
/// </summary>
public class CreateUserCommand : IRequest<Models.User> {
  private string _name = string.Empty;

  private string _email = string.Empty;

  private bool _isActive = true;

  public string Name {
    get => _name;
    set => _name = value;
  }

  public string Email {
    get => _email;
    set => _email = value;
  }

  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }
}
