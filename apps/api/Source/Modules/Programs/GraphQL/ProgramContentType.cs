using GameGuild.Common.Enums;
using Microsoft.EntityFrameworkCore;
using ProgramContentEntity = GameGuild.Modules.Program.Models.ProgramContent;


namespace GameGuild.Modules.Program.GraphQL;

/// <summary>
/// GraphQL type definition for ProgramContent entity
/// </summary>
public class ProgramContentType : ObjectType<ProgramContentEntity> {
  protected override void Configure(IObjectTypeDescriptor<ProgramContentEntity> descriptor) {
    descriptor.Name("ProgramContent");
    descriptor.Description("Represents program content in the CMS system with hierarchical structure.");

    // Base Entity Properties
    descriptor.Field(p => p.Id)
              .Type<NonNullType<UuidType>>()
              .Description("The unique identifier for the program content (UUID).");

    descriptor.Field(p => p.Version).Description("Version control for optimistic concurrency.");

    descriptor.Field(p => p.CreatedAt)
              .Type<NonNullType<DateTimeType>>()
              .Description("The date and time when the program content was created.");

    descriptor.Field(p => p.UpdatedAt)
              .Type<DateTimeType>()
              .Description("The date and time when the program content was last updated.");

    descriptor.Field(p => p.DeletedAt)
              .Type<DateTimeType>()
              .Description("The date and time when the program content was soft deleted (null if not deleted).");

    descriptor.Field(p => p.IsDeleted)
              .Type<NonNullType<BooleanType>>()
              .Description("Indicates whether the program content has been soft deleted.");

    // Program Content Specific Properties
    descriptor.Field(p => p.ProgramId)
              .Type<NonNullType<UuidType>>()
              .Description("The unique identifier of the parent program.");

    descriptor.Field(p => p.ParentId)
              .Type<UuidType>()
              .Description("The unique identifier of the parent content (null for root content).");

    descriptor.Field(p => p.Title).Type<NonNullType<StringType>>().Description("The title of the program content.");

    descriptor.Field(p => p.Type).Type<NonNullType<StringType>>().Description("The type of content.");

    descriptor.Field(p => p.Body).Type<StringType>().Description("The main content body (JSON format).");

    descriptor.Field(p => p.Description).Type<StringType>().Description("Description of the content.");

    descriptor.Field(p => p.EstimatedMinutes).Type<IntType>().Description("Estimated duration in minutes.");

    descriptor.Field(p => p.Visibility)
              .Type<NonNullType<EnumType<Visibility>>>()
              .Description("The publication status of the content.");

    descriptor.Field(p => p.SortOrder)
              .Type<NonNullType<IntType>>()
              .Description("The sort order within the parent container.");

    descriptor.Field(p => p.IsRequired)
              .Type<NonNullType<BooleanType>>()
              .Description("Indicates whether this content is required for program completion.");

    descriptor.Field(p => p.GradingMethod)
              .Type<StringType>()
              .Description("How this content should be graded (if applicable).");

    descriptor.Field(p => p.MaxPoints)
              .Type<DecimalType>()
              .Description("Maximum points/score for this content (if gradeable).");

    // Navigation Properties
    descriptor.Field(p => p.Program)
              .Type<ObjectType<Models.Program>>()
              .Description("The parent program this content belongs to.")
              .ResolveWith<ProgramContentResolvers>(r => r.GetProgramAsync(default!, default!));

    descriptor.Field(p => p.Parent)
              .Type<ObjectType<ProgramContentEntity>>()
              .Description("The parent content (null for root content).")
              .ResolveWith<ProgramContentResolvers>(r => r.GetParentContentAsync(default!, default!));

    descriptor.Field("childContents")
              .Type<ListType<ObjectType<ProgramContentEntity>>>()
              .Description("Child contents under this content.")
              .ResolveWith<ProgramContentResolvers>(r => r.GetChildContentsAsync(default!, default!));
  }
}

/// <summary>
/// Resolvers for ProgramContent navigation properties
/// </summary>
public class ProgramContentResolvers {
  /// <summary>
  /// Resolves the parent program for the content
  /// </summary>
  public async Task<Models.Program?> GetProgramAsync(
    [Parent] ProgramContentEntity content,
    [Service] Data.ApplicationDbContext context
  ) {
    return await context.Programs.FirstOrDefaultAsync(p => p.Id == content.ProgramId);
  }

  /// <summary>
  /// Resolves the parent content for hierarchical structure
  /// </summary>
  public async Task<ProgramContentEntity?> GetParentContentAsync(
    [Parent] ProgramContentEntity content,
    [Service] Data.ApplicationDbContext context
  ) {
    if (!content.ParentId.HasValue) return null;

    return await context.ProgramContents.FirstOrDefaultAsync(pc => pc.Id == content.ParentId.Value);
  }

  /// <summary>
  /// Resolves child contents for hierarchical structure
  /// </summary>
  public async Task<IEnumerable<ProgramContentEntity>> GetChildContentsAsync(
    [Parent] ProgramContentEntity content,
    [Service] Data.ApplicationDbContext context
  ) {
    return await context.ProgramContents.Where(pc => pc.ParentId == content.Id)
                        .OrderBy(pc => pc.SortOrder)
                        .ToListAsync();
  }
}
