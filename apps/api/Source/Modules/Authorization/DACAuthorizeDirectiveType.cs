namespace GameGuild.Source.Modules.Authorization;

/// <summary> GraphQL directive for 3-layer DAC authorization </summary>
public class DacAuthorizeDirectiveType : DirectiveType {
  protected override void Configure(IDirectiveTypeDescriptor descriptor) {
    descriptor.Name("dacAuthorize");
    descriptor.Location(DirectiveLocation.FieldDefinition);
    descriptor.Argument("level").Type<EnumType<GameGuild.Authorization.DacPermissionLevel>>();
    descriptor.Argument("permission").Type<StringType>();
    descriptor.Argument("entityType").Type<StringType>();
  }
}
