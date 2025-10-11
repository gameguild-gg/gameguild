using GameGuild.Modules.Contents;
using GameGuild.Modules.Tenants;

namespace GameGuild.Modules.Programs;

/// <summary>
/// GraphQL type definition for Program entity
/// </summary>
public class ProgramType : ObjectType<Program> {
    protected override void Configure(IObjectTypeDescriptor<Program> descriptor) {
        descriptor.Description("Represents a learning program with structured educational content");

        // Base entity fields
        descriptor.Field(p => p.Id)
                  .Type<NonNullType<UuidType>>()
                  .Description("The unique identifier for the program");

        descriptor.Field(p => p.Version)
                  .Type<NonNullType<IntType>>()
                  .Description("Version control for optimistic concurrency");

        descriptor.Field(p => p.CreatedAt)
                  .Type<NonNullType<DateTimeType>>()
                  .Description("When the program was created");

        descriptor.Field(p => p.UpdatedAt)
                  .Type<DateTimeType>()
                  .Description("When the program was last updated");

        descriptor.Field(p => p.DeletedAt)
                  .Type<DateTimeType>()
                  .Description("When the program was soft deleted (null if not deleted)");

        descriptor.Field(p => p.IsDeleted)
                  .Type<NonNullType<BooleanType>>()
                  .Description("Whether the program has been soft deleted");

        // Content fields (inherited from Content)
        descriptor.Field(p => p.Title)
                  .Type<NonNullType<StringType>>()
                  .Description("The title of the program");

        descriptor.Field(p => p.Description)
                  .Type<StringType>()
                  .Description("Detailed description of the program");

        descriptor.Field(p => p.Slug)
                  .Type<NonNullType<StringType>>()
                  .Description("URL-friendly identifier for the program");

        descriptor.Field(p => p.Status)
                  .Type<NonNullType<EnumType<ContentStatus>>>()
                  .Description("The publication status of the program");

        descriptor.Field(p => p.Visibility)
                  .Type<NonNullType<EnumType<AccessLevel>>>()
                  .Description("The access level of the program");

        // Program-specific fields
        descriptor.Field(p => p.Thumbnail)
                  .Type<StringType>()
                  .Description("Thumbnail image URL for program display");

        descriptor.Field(p => p.VideoShowcaseUrl)
                  .Type<StringType>()
                  .Description("Video showcase URL for program preview");

        descriptor.Field(p => p.Category)
                  .Type<EnumType<ProgramCategory>>()
                  .Description("The category/domain of the program");

        descriptor.Field(p => p.Difficulty)
                  .Type<EnumType<ProgramDifficulty>>()
                  .Description("The difficulty level of the program");

        descriptor.Field(p => p.EstimatedHours)
                  .Type<FloatType>()
                  .Description("Estimated time in hours required to complete the program");

        // Navigation properties
        descriptor.Field(p => p.Tenant)
                  .Type<TenantType>()
                  .Description("The tenant this program belongs to");
    }
}
