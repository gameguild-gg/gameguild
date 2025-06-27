namespace GameGuild.Modules.UserProfile.GraphQL;

/// <summary>
/// Input for creating a user profile
/// </summary>
public class CreateUserProfileInput {
  private string? _givenName;

  private string? _familyName;

  private string? _displayName;

  private string? _title;

  private string? _description;

  private string? _slug;

  private Guid? _tenantId;

  public string? GivenName {
    get => _givenName;
    set => _givenName = value;
  }

  public string? FamilyName {
    get => _familyName;
    set => _familyName = value;
  }

  public string? DisplayName {
    get => _displayName;
    set => _displayName = value;
  }

  public string? Title {
    get => _title;
    set => _title = value;
  }

  public string? Description {
    get => _description;
    set => _description = value;
  }

  public string? Slug {
    get => _slug;
    set => _slug = value;
  }

  public Guid? TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }
}

/// <summary>
/// Input for updating a user profile
/// </summary>
public class UpdateUserProfileInput {
  private Guid _id;

  private string? _givenName;

  private string? _familyName;

  private string? _displayName;

  private string? _title;

  private string? _description;

  private string? _slug;

  private Guid? _tenantId;

  public Guid Id {
    get => _id;
    set => _id = value;
  }

  public string? GivenName {
    get => _givenName;
    set => _givenName = value;
  }

  public string? FamilyName {
    get => _familyName;
    set => _familyName = value;
  }

  public string? DisplayName {
    get => _displayName;
    set => _displayName = value;
  }

  public string? Title {
    get => _title;
    set => _title = value;
  }

  public string? Description {
    get => _description;
    set => _description = value;
  }

  public string? Slug {
    get => _slug;
    set => _slug = value;
  }

  public Guid? TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }
}
