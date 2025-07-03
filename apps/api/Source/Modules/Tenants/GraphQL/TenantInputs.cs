using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenant.GraphQL;

/// <summary>
/// Input type for creating a new tenant
/// </summary>
public class CreateTenantInput {
  private string _name = string.Empty;

  private string? _description;

  private bool _isActive = true;

  /// <summary>
  /// Name of the tenant
  /// </summary>
  [Required]
  [StringLength(100)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  /// <summary>
  /// Description of the tenant
  /// </summary>
  [StringLength(500)]
  public string? Description {
    get => _description;
    set => _description = value;
  }

  /// <summary>
  /// Whether this tenant is currently active
  /// </summary>
  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }
}

/// <summary>
/// Input type for updating an existing tenant
/// </summary>
public class UpdateTenantInput {
  private Guid _id;

  private string? _name;

  private string? _description;

  private bool? _isActive;

  /// <summary>
  /// ID of the tenant to update
  /// </summary>
  [Required]
  public Guid Id {
    get => _id;
    set => _id = value;
  }

  /// <summary>
  /// Name of the tenant
  /// </summary>
  [StringLength(100)]
  public string? Name {
    get => _name;
    set => _name = value;
  }

  /// <summary>
  /// Description of the tenant
  /// </summary>
  [StringLength(500)]
  public string? Description {
    get => _description;
    set => _description = value;
  }

  /// <summary>
  /// Whether this tenant is currently active
  /// </summary>
  public bool? IsActive {
    get => _isActive;
    set => _isActive = value;
  }
}

/// <summary>
/// Input type for adding a user to a tenant
/// </summary>
public class AddUserToTenantInput {
  private Guid _tenantId;

  private Guid _userId;

  /// <summary>
  /// ID of the tenant
  /// </summary>
  [Required]
  public Guid TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }

  /// <summary>
  /// ID of the user
  /// </summary>
  [Required]
  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }
}

/// <summary>
/// Input type for removing a user from a tenant
/// </summary>
public class RemoveUserFromTenantInput {
  private Guid _tenantId;

  private Guid _userId;

  /// <summary>
  /// ID of the tenant
  /// </summary>
  [Required]
  public Guid TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }

  /// <summary>
  /// ID of the user
  /// </summary>
  [Required]
  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }
}
