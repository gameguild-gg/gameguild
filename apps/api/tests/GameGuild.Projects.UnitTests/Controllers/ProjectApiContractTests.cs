using System.Text.Json;
using GameGuild.Projects;
using GameGuild.Teams;

namespace GameGuild.Projects.UnitTests.Controllers;

public sealed class ProjectApiContractTests
{
    [Fact]
    public void FromProject_PreservesPublicNavigationDataWithoutSerializingEntityBackReferences()
    {
        var projectId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            TenantId = tenantId,
            Title = "Project Atlas",
            Slug = "project-atlas",
            Status = ContentStatus.Published,
            Visibility = ContentVisibility.Public,
            CreatedById = userId,
            Category = new ProjectCategory { Id = Guid.NewGuid(), Name = "Games" },
            ProjectMetadata = new ProjectMetadata
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                ViewCount = 17,
                DownloadCount = 3,
                FollowerCount = 5,
            },
        };
        project.Versions.Add(new ProjectVersion
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            VersionNumber = "1.2.0",
            Status = "published",
            CreatedById = userId,
        });
        project.Collaborators.Add(new ProjectCollaborator
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            UserId = userId,
            Role = "Editor",
            Permissions = "Read,Edit",
            IsActive = true,
        });
        project.Teams.Add(new ProjectTeam
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            TeamId = teamId,
            Team = Team.Create(tenantId, "Atlas Team", "atlas-team", userId),
            Role = ProjectTeamRole.Owner,
            ParticipationMode = ProjectTeamParticipationMode.AllMembers,
            IsActive = true,
        });

        var response = ProjectApiResponse.FromProject(project);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(projectId, response.Id);
        Assert.Equal("Games", response.Category?.Name);
        Assert.Equal(17, response.Metadata?.ViewCount);
        Assert.Equal("1.2.0", Assert.Single(response.Versions).VersionNumber);
        Assert.Equal(userId, Assert.Single(response.Collaborators).UserId);
        Assert.Equal("Atlas Team", Assert.Single(response.Teams).Name);
        Assert.DoesNotContain("\"project\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"createdBy\"", json, StringComparison.OrdinalIgnoreCase);
    }
}
