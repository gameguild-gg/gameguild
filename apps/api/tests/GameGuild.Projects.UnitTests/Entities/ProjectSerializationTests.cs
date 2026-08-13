using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Projects.UnitTests.Entities;

public sealed class ProjectSerializationTests
{
    private static readonly JsonSerializerOptions ApiJsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    [Fact]
    public void Project_Response_Omits_Unloaded_Navigation_Properties()
    {
        var project = new Project
        {
            Title = "Public project",
            Slug = "public-project",
            Status = ContentStatus.Published,
            Visibility = ContentVisibility.Public,
            Collaborators =
            [
                new ProjectCollaborator
                {
                    ProjectId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Role = "Viewer",
                    Permissions = "Read"
                }
            ],
            Releases =
            [
                new ProjectRelease
                {
                    ProjectId = Guid.NewGuid(),
                    Title = "Initial release",
                    ReleaseVersion = "1.0.0"
                }
            ]
        };

        var json = JsonSerializer.SerializeToElement(project, ApiJsonOptions);

        json.TryGetProperty("category", out _).Should().BeFalse();
        json.TryGetProperty("projectMetadata", out _).Should().BeFalse();
        json.TryGetProperty("latestVersion", out _).Should().BeFalse();

        var collaborator = json.GetProperty("collaborators")[0];
        collaborator.TryGetProperty("project", out _).Should().BeFalse();
        collaborator.TryGetProperty("user", out _).Should().BeFalse();

        var release = json.GetProperty("releases")[0];
        release.TryGetProperty("project", out _).Should().BeFalse();
    }
}
