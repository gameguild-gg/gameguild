namespace GameGuild.Common.GraphQL.Authorization;

/// <summary>
/// GraphQL directive for 3-layer DAC authorization
/// </summary>
public class DACAuthorizeDirectiveType : DirectiveType {
  protected override void Configure(IDirectiveTypeDescriptor descriptor) {
    descriptor.Name("dacAuthorize");
    descriptor.Location(DirectiveLocation.FieldDefinition);
    descriptor.Argument("level").Type<EnumType<DACPermissionLevel>>();
    descriptor.Argument("permission").Type<StringType>();
    descriptor.Argument("entityType").Type<StringType>();
  }
}

/// <summary>
/// Extension methods for applying DAC authorization middleware
/// </summary>
public static class DACAuthorizationExtensions {
  /// <summary>
  /// Adds DAC authorization middleware to a field
  /// </summary>
  public static IObjectFieldDescriptor UseDACAuthorization(this IObjectFieldDescriptor descriptor) { return descriptor.Use<DACAuthorizationMiddleware>(); }
}
