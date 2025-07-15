using GameGuild.Modules.UserProfiles;
using MediatR;


namespace GameGuild.Modules.Users;

public class UserType : ObjectType<User> {
  protected override void Configure(IObjectTypeDescriptor<User> descriptor) {
    descriptor.Description("Represents a user in the CMS system with full EntityBase support.");

    // Base Entity Properties
    descriptor.Field(u => u.Id).Type<IdType>().Description("The unique identifier for the user (UUID).");

    descriptor.Field(u => u.Version).Description("Version control for optimistic concurrency.");

    descriptor.Field(u => u.CreatedAt).Description("The date and time when the user was created.");

    descriptor.Field(u => u.UpdatedAt).Description("The date and time when the user was last updated.");

    descriptor.Field(u => u.DeletedAt).Description("The date and time when the user was soft deleted (null if not deleted).");

    descriptor.Field(u => u.IsDeleted).Description("Indicates whether the user has been soft deleted.");

    // User-specific Properties
    descriptor.Field(u => u.Name).Description("The name of the user.");

    descriptor.Field(u => u.Email).Description("The email address of the user.");

    descriptor.Field(u => u.IsActive).Description("Indicates whether the user is active.");

    // Profile relationship
    descriptor.Field("profile")
              .Type<UserProfileType>()
              .Description("The user's profile information.")
              .ResolveWith<UserResolvers>(r => r.GetProfileAsync(default!, default!));
  }
}

public class UserResolvers {
  public async Task<UserProfile?> GetProfileAsync(
    [Parent] User user,
    [Service] IMediator mediator
  ) {
    var query = new GetUserProfileByUserIdQuery { UserId = user.Id, IncludeDeleted = false };

    var result = await mediator.Send(query);

    return result.IsSuccess ? result.Value : null;
  }
}
