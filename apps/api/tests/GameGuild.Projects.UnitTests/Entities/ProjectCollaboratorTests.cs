namespace GameGuild.Projects.UnitTests.Entities;

public class ProjectCollaboratorTests
{
    [Fact]
    public void ProjectCollaborator_Creation_Should_Set_Default_Values()
    {
        // Arrange & Act
        var collaborator = new ProjectCollaborator();

        // Assert
        collaborator.Role.Should().BeEmpty();
        collaborator.Permissions.Should().BeEmpty();
        collaborator.IsActive.Should().BeTrue();
        collaborator.Id.Should().BeEmpty();
        collaborator.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        collaborator.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(ProjectRoles.Owner)]
    [InlineData(ProjectRoles.Editor)]
    [InlineData(ProjectRoles.Viewer)]
    public void ProjectCollaborator_Should_Accept_Valid_Roles(string role)
    {
        // Arrange & Act
        var collaborator = new ProjectCollaborator
        {
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = role,
            IsActive = true
        };

        // Assert
        collaborator.Role.Should().Be(role);
    }

    [Theory]
    [InlineData("Read,Write,Delete")]
    [InlineData("Read")]
    [InlineData("Read,Write")]
    public void ProjectCollaborator_Should_Accept_Valid_Permissions(string permissions)
    {
        // Arrange & Act
        var collaborator = new ProjectCollaborator
        {
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = ProjectRoles.Editor,
            Permissions = permissions,
            IsActive = true
        };

        // Assert
        collaborator.Permissions.Should().Be(permissions);
    }

    [Fact]
    public void ProjectCollaborator_Should_Have_Required_Relationships()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Act
        var collaborator = new ProjectCollaborator
        {
            ProjectId = projectId,
            UserId = userId,
            Role = ProjectRoles.Owner,
            Permissions = "All",
            IsActive = true
        };

        // Assert
        collaborator.ProjectId.Should().Be(projectId);
        collaborator.UserId.Should().Be(userId);
    }

    [Fact]
    public void ProjectCollaborator_Should_Track_Join_Date()
    {
        // Arrange
        var joinDate = DateTime.UtcNow.AddDays(-5);
        
        // Act
        var collaborator = new ProjectCollaborator
        {
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = ProjectRoles.Viewer,
            JoinedAt = joinDate,
            IsActive = true
        };

        // Assert
        collaborator.JoinedAt.Should().Be(joinDate);
    }

    [Fact]
    public void ProjectCollaborator_Should_Track_Optional_End_Date()
    {
        var leftAt = DateTime.UtcNow;

        // Arrange & Act
        var collaborator = new ProjectCollaborator
        {
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = ProjectRoles.Editor,
            LeftAt = leftAt,
            IsActive = true
        };

        // Assert
        collaborator.LeftAt.Should().Be(leftAt);
    }

    [Fact]
    public void ProjectCollaborator_Should_Allow_Null_Optional_Fields()
    {
        // Arrange & Act
        var collaborator = new ProjectCollaborator
        {
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = ProjectRoles.Viewer,
            IsActive = true
        };

        // Assert
        collaborator.LeftAt.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProjectCollaborator_Should_Support_Active_Status(bool isActive)
    {
        // Arrange & Act
        var collaborator = new ProjectCollaborator
        {
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = ProjectRoles.Editor,
            IsActive = isActive
        };

        // Assert
        collaborator.IsActive.Should().Be(isActive);
    }

    [Fact]
    public void ProjectCollaborator_Should_Have_Audit_Fields()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        // Act
        var collaborator = new ProjectCollaborator
        {
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = ProjectRoles.Owner,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        collaborator.CreatedAt.Should().Be(now);
        collaborator.UpdatedAt.Should().Be(now);
        collaborator.Id.Should().BeEmpty();
    }
}
