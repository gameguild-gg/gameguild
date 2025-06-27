namespace GameGuild.Modules.Auth.Dtos;

/// <summary>
/// Tenant information included in authentication responses
/// </summary>
public class TenantInfoDto {
  private Guid _id;

  private string _name = string.Empty;

  private bool _isActive;

  /// <summary>
  /// Tenant ID
  /// </summary>
  public Guid Id {
    get => _id;
    set => _id = value;
  }

  /// <summary>
  /// Tenant name
  /// </summary>
  public string Name {
    get => _name;
    set => _name = value;
  }

  /// <summary>
  /// Whether tenant is active
  /// </summary>
  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }
}
