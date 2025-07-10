using System.Security.Claims;
using GameGuild.Common;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Projects.Models;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Projects.GraphQL;

/// <summary>
/// GraphQL type definition for Project entity with DAC permission integration
/// </summary>
public class ProjectType : ObjectType<Project> {
  protected override void Configure(IObjectTypeDescriptor<Project> descriptor) {
    descriptor.Name("Project");
    descriptor.Description(
      "Represents a project in the CMS system with full ResourceBase support and DAC permissions."
    );

    // Base Entity Properties
    descriptor.Field(p => p.Id)
              .Type<NonNullType<UuidType>>()
              .Description("The unique identifier for the project (UUID).");

    descriptor.Field(p => p.Version).Description("Version control for optimistic concurrency.");

    descriptor.Field(p => p.CreatedAt)
              .Type<NonNullType<DateTimeType>>()
              .Description("The date and time when the project was created.");

    descriptor.Field(p => p.UpdatedAt)
              .Type<DateTimeType>()
              .Description("The date and time when the project was last updated.");

    descriptor.Field(p => p.DeletedAt)
              .Type<DateTimeType>()
              .Description("The date and time when the project was soft deleted (null if not deleted).");

    descriptor.Field(p => p.IsDeleted)
              .Type<NonNullType<BooleanType>>()
              .Description("Indicates whether the project has been soft deleted.");

    // ResourceBase Properties
    descriptor.Field(p => p.Title).Type<NonNullType<StringType>>().Description("The title/name of the project.");

    descriptor.Field(p => p.Description).Type<StringType>().Description("Optional description of the project.");

    descriptor.Field(p => p.Visibility)
              .Type<NonNullType<EnumType<AccessLevel>>>()
              .Description("Access level of the project (Public, Private, Restricted, etc.).");

    // Content Properties
    descriptor.Field(p => p.Slug)
              .Type<NonNullType<StringType>>()
              .Description("URL-friendly unique identifier for the project.");

    descriptor.Field(p => p.Status)
              .Type<NonNullType<EnumType<ContentStatus>>>()
              .Description("Status of the project (draft, published, etc.).");

    // Project-specific Properties
    descriptor.Field(p => p.ShortDescription).Type<StringType>().Description("Short description (max 500 chars).");

    descriptor.Field(p => p.WebsiteUrl).Type<StringType>().Description("Project website URL.");

    descriptor.Field(p => p.RepositoryUrl).Type<StringType>().Description("Source code repository URL.");

    descriptor.Field(p => p.SocialLinks).Type<StringType>().Description("JSON object containing social media links.");

    // Navigation Properties
    descriptor.Field(p => p.Category).Type<NonNullType<ObjectType<ProjectCategory>>>().Description("Project category.");

    descriptor.Field(p => p.CreatedBy)
              .Type<ObjectType<User>>()
              .Description("User who created this project.");

    descriptor.Field(p => p.ProjectMetadata)
              .Type<ListType<ObjectType<ProjectMetadata>>>()
              .Description("Additional metadata for the project.");

    descriptor.Field(p => p.Versions)
              .Type<ListType<ObjectType<ProjectVersion>>>()
              .Description("Project versions and releases.");

    // Computed properties for permissions
    descriptor.Field("canEdit")
              .Type<NonNullType<BooleanType>>()
              .Description("Whether the current user can edit this project.")
              .Resolve(async context => {
                  var permissionService = context.Service<IPermissionService>();
                  var project = context.Parent<Project>();
                  var user = context.GetUser();

                  if (user?.Identity?.IsAuthenticated != true) return false;

                  var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                  var tenantIdClaim = user.FindFirst(JwtClaimTypes.TenantId)?.Value;
                  var tenantId = tenantIdClaim != null ? Guid.Parse(tenantIdClaim) : (Guid?)null;

                  return await permissionService.HasResourcePermissionAsync<ProjectPermission, Project>(
                           userId,
                           tenantId,
                           project.Id,
                           PermissionType.Edit
                         );
                }
              );

    descriptor.Field("canDelete")
              .Type<NonNullType<BooleanType>>()
              .Description("Whether the current user can delete this project.")
              .Resolve(async context => {
                  var permissionService = context.Service<IPermissionService>();
                  var project = context.Parent<Project>();
                  var user = context.GetUser();

                  if (user?.Identity?.IsAuthenticated != true) return false;

                  var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                  var tenantIdClaim = user.FindFirst(JwtClaimTypes.TenantId)?.Value;
                  var tenantId = tenantIdClaim != null ? Guid.Parse(tenantIdClaim) : (Guid?)null;

                  return await permissionService.HasResourcePermissionAsync<ProjectPermission, Project>(
                           userId,
                           tenantId,
                           project.Id,
                           PermissionType.Delete
                         );
                }
              );
  }
}
