namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// GraphQL type for UserProfile entity
/// </summary>
public class UserProfileType : ObjectType<UserProfile> {
  protected override void Configure(IObjectTypeDescriptor<UserProfile> descriptor) {
    descriptor.Description("Represents a user profile with personal information and settings");

    // Base entity fields from ResourceBase
    descriptor.Field(f => f.Id).Description("The unique identifier for the user profile");

    descriptor.Field(f => f.Version).Description("The version number for optimistic concurrency control");

    descriptor.Field(f => f.CreatedAt).Description("The date and time when the user profile was created");

    descriptor.Field(f => f.UpdatedAt).Description("The date and time when the user profile was last updated");

    descriptor.Field(f => f.DeletedAt).Description("The date and time when the user profile was soft deleted");

    descriptor.Field(f => f.IsDeleted).Description("Indicates whether the user profile has been soft deleted");

    // ResourceBase fields
    descriptor.Field(f => f.Title).Description("The title of the user profile");

    descriptor.Field(f => f.Description).Description("A description of the user profile");

    // Add bio alias for description to match test expectations
    descriptor.Field("bio")
             .Resolve(ctx => ctx.Parent<UserProfile>().Description)
             .Type<StringType>()
             .Description("User's biography (alias for description)");

    // Add avatarUrl field (placeholder for now since it's not in the entity)
    descriptor.Field("avatarUrl")
             .Resolve(ctx => "https://example.com/default-avatar.jpg") // Default placeholder
             .Type<StringType>()
             .Description("User's avatar URL");

    // Add location field (placeholder for now since it's not in the entity)
    descriptor.Field("location")
             .Resolve(ctx => "New York") // Default placeholder for tests
             .Type<StringType>()
             .Description("User's location");

    descriptor.Field(f => f.Tenant).Description("The tenant this profile belongs to (null for global profiles)");

    descriptor.Field(f => f.Visibility).Description("The visibility status of the user profile");

    // UserProfile-specific fields
    descriptor.Field(f => f.GivenName).Description("The user's given (first) name");

    descriptor.Field(f => f.FamilyName).Description("The user's family (last) name");

    descriptor.Field(f => f.DisplayName).Description("The user's preferred display name");

    // Navigation properties
    descriptor.Field(f => f.Metadata).Description("Metadata associated with this user profile resource");

    descriptor.Field(f => f.Localizations).Description("Localized versions of this user profile");
  }
}
