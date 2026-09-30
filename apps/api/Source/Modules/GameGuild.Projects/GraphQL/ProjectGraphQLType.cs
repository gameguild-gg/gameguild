using HotChocolate.Types;

namespace GameGuild.Projects;

/// <summary>
///     Publishes an explicit, safe GraphQL projection for projects instead of reflecting every
///     persistence property and inherited domain-event/navigation member into the schema.
/// </summary>
public sealed class ProjectGraphQLType : ObjectType<Project>
{
    protected override void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(project => project.Id);
        descriptor.Field(project => project.Title);
        descriptor.Field(project => project.Slug);
        descriptor.Field(project => project.ShortDescription);
        descriptor.Field(project => project.Description);
        descriptor.Field(project => project.ImageUrl);
        descriptor.Field(project => project.Type);
        descriptor.Field(project => project.DevelopmentStatus);
        descriptor.Field(project => project.Status);
        descriptor.Field(project => project.Visibility);
        descriptor.Field(project => project.CategoryId);
        descriptor.Field(project => project.WebsiteUrl);
        descriptor.Field(project => project.RepositoryUrl);
        descriptor.Field(project => project.DownloadUrl);
        descriptor.Field(project => project.Tags);
        descriptor.Field(project => project.FeaturedImageUrl);
        descriptor.Field(project => project.License);
        descriptor.Field(project => project.Copyright);
        descriptor.Field(project => project.PublishedAt);
        descriptor.Field(project => project.CreatedAt);
        descriptor.Field(project => project.UpdatedAt);
    }
}
