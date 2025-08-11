namespace GameGuild.Modules.Tenants;

/// <summary>
/// GraphQL type for Tenant entity
/// </summary>
public class TenantType : ObjectType<Tenant> {
  protected override void Configure(IObjectTypeDescriptor<Tenant> descriptor) {
    descriptor.Name("Tenant");
    descriptor.Description("A tenant represents an organization or group within the system");

    // Basic fields
    descriptor.Field(t => t.Id).Type<NonNullType<UuidType>>().Description("The unique identifier of the tenant");

    descriptor.Field(t => t.Name).Type<NonNullType<StringType>>().Description("The name of the tenant");

    descriptor.Field(t => t.Description).Type<StringType>().Description("The description of the tenant");

    descriptor.Field(t => t.IsActive).Type<NonNullType<BooleanType>>().Description("Whether the tenant is active");

    // BaseEntity fields
    descriptor.Field(t => t.CreatedAt)
              .Type<NonNullType<DateTimeType>>()
              .Description("The date and time when the tenant was created");

    descriptor.Field(t => t.UpdatedAt)
              .Type<DateTimeType>()
              .Description("The date and time when the tenant was last updated");

    descriptor.Field(t => t.Version)
              .Type<NonNullType<IntType>>()
              .Description("The version number for optimistic concurrency control");

    // Navigation properties
    descriptor.Field(t => t.TenantPermissions)
              .Type<ListType<TenantPermissionType>>()
              .Description("The users and their permissions associated with this tenant");
  }
}
