using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.User.GraphQL;

public class CreateUserInput {
  private string _name = string.Empty;

  private string _email = string.Empty;

  private bool _isActive = true;

  [Required]
  [StringLength(100)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  [Required]
  [EmailAddress]
  [StringLength(255)]
  public string Email {
    get => _email;
    set => _email = value;
  }

  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }
}

public class UpdateUserInput {
  private Guid _id;

  private string? _name;

  private string? _email;

  private bool? _isActive;

  [Required]
  public Guid Id {
    get => _id;
    set => _id = value;
  }

  [StringLength(100)]
  public string? Name {
    get => _name;
    set => _name = value;
  }

  [EmailAddress]
  [StringLength(255)]
  public string? Email {
    get => _email;
    set => _email = value;
  }

  public bool? IsActive {
    get => _isActive;
    set => _isActive = value;
  }
}
