using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.User.Dtos;

public class CreateUserDto {
  private string _name = string.Empty;

  private string _email = string.Empty;

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
}

public class UpdateUserDto {
  private string? _name;

  private string? _email;

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
}

public class UserResponseDto {
  private Guid _id;

  private int _version;

  private string _name = string.Empty;

  private string _email = string.Empty;

  private bool _isActive;

  private DateTime _createdAt;

  private DateTime _updatedAt;

  private DateTime? _deletedAt;

  private bool _isDeleted;

  public Guid Id {
    get => _id;
    set => _id = value;
  }

  public int Version {
    get => _version;
    set => _version = value;
  }

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

  public DateTime CreatedAt {
    get => _createdAt;
    set => _createdAt = value;
  }

  public DateTime UpdatedAt {
    get => _updatedAt;
    set => _updatedAt = value;
  }

  public DateTime? DeletedAt {
    get => _deletedAt;
    set => _deletedAt = value;
  }

  public bool IsDeleted {
    get => _isDeleted;
    set => _isDeleted = value;
  }
}
