using GameGuild.Modules.Users;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// GraphQL type for TenantPermission entity
/// </summary>
public class TenantPermissionType : ObjectType<TenantPermission> {
  protected override void Configure(IObjectTypeDescriptor<TenantPermission> descriptor) {
    descriptor.Name("TenantPermission");
    descriptor.Description("Represents the permissions and relationship between a user and a tenant");

    descriptor.Field(tp => tp.Id)
              .Type<NonNullType<UuidType>>()
              .Description("The unique identifier of the tenant permission");

    descriptor.Field(tp => tp.UserId).Type<NonNullType<UuidType>>().Description("The user identifier");

    descriptor.Field(tp => tp.TenantId).Type<NonNullType<UuidType>>().Description("The tenant identifier");

    descriptor.Field(tp => tp.CreatedAt)
              .Type<NonNullType<DateTimeType>>()
              .Description("The date and time when the user joined the tenant");

    descriptor.Field(tp => tp.ExpiresAt)
              .Type<DateTimeType>()
              .Description("The date and time when the permission expires");

    descriptor.Field(tp => tp.PermissionFlags1)
              .Type<NonNullType<LongType>>()
              .Description("Permission flags for bits 0-63");

    descriptor.Field(tp => tp.PermissionFlags2)
              .Type<NonNullType<LongType>>()
              .Description("Permission flags for bits 64-127");

    // Navigation properties
    descriptor.Field(tp => tp.User).Type<UserType>().Description("The user in this relationship");

    descriptor.Field(tp => tp.Tenant).Type<TenantType>().Description("The tenant in this relationship");
  }
}
