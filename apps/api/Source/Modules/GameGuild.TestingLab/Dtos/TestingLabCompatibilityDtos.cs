namespace GameGuild.TestingLab;

public sealed class PermissionConstraint {
  public string Type { get; set; } = string.Empty;
  public string Value { get; set; } = string.Empty;
}

public sealed class PermissionTemplate {
  public string Action { get; set; } = string.Empty;
  public string ResourceType { get; set; } = string.Empty;
}

public sealed class RoleTemplate {
  public Guid Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public bool IsSystemRole { get; set; }
  public List<PermissionTemplate>? PermissionTemplates { get; set; }
}
