using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GameGuild.Projects.UnitTests.Configuration;

public sealed class ProjectsModelConfigurationTests
{
    [Fact]
    public void Configure_Should_Register_Project_Runtime_Entities()
    {
        var modelBuilder = new ModelBuilder();

        new ProjectsModelConfiguration().Configure(modelBuilder);

        Entity<ProjectCategory>(modelBuilder).GetTableName().Should().Be("project_categories");
        Entity<ProjectMetadata>(modelBuilder).GetTableName().Should().Be("project_metadata");
        Entity<ProjectRelease>(modelBuilder).GetTableName().Should().Be("project_releases");
        Entity<ProjectVersion>(modelBuilder).GetTableName().Should().Be("project_versions");
        Entity<ProjectTeam>(modelBuilder).GetTableName().Should().Be("project_teams");
        Entity<ProjectInvitation>(modelBuilder).GetTableName().Should().Be("project_invitations");

        Entity<ProjectMetadata>(modelBuilder).GetIndexes().Should().ContainSingle(index =>
            index.IsUnique &&
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(ProjectMetadata.ProjectId));
        Entity<ProjectInvitation>(modelBuilder).FindProperty(nameof(ProjectInvitation.Token))!.GetMaxLength().Should().Be(64);
        Entity<ProjectInvitation>(modelBuilder).FindProperty(nameof(ProjectInvitation.InvitedEmail))!.GetMaxLength().Should().Be(255);
        Entity<ProjectInvitation>(modelBuilder).FindProperty(nameof(ProjectInvitation.Role))!.GetMaxLength().Should().Be(100);
        Entity<ProjectInvitation>(modelBuilder).FindProperty(nameof(ProjectInvitation.Permissions))!.GetMaxLength().Should().Be(500);
    }

    [Fact]
    public void Configure_Should_Register_Project_Collaboration_Team_Schema()
    {
        var modelBuilder = new ModelBuilder();

        new ProjectsModelConfiguration().Configure(modelBuilder);

        Entity<Team>(modelBuilder).GetTableName().Should().Be("project_collaboration_teams");
        Entity<TeamMember>(modelBuilder).GetTableName().Should().Be("project_collaboration_team_members");

        Entity<Team>(modelBuilder).FindProperty(nameof(Team.Name))!.GetMaxLength().Should().Be(200);
        Entity<Team>(modelBuilder).FindProperty(nameof(Team.Description))!.GetMaxLength().Should().Be(2000);
        Entity<TeamMember>(modelBuilder).FindProperty(nameof(TeamMember.Role))!.GetMaxLength().Should().Be(100);

        Entity<ProjectTeam>(modelBuilder).GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Team) &&
            foreignKey.Properties.Count == 1 &&
            foreignKey.Properties[0].Name == nameof(ProjectTeam.TeamId));

        Entity<TeamMember>(modelBuilder).GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Team) &&
            foreignKey.Properties.Count == 1 &&
            foreignKey.Properties[0].Name == nameof(TeamMember.TeamId));
    }

    [Fact]
    public void Configure_Should_Not_Create_Shadow_Project_Foreign_Keys_For_Project_Relationships()
    {
        using var context = CreateFinalizedConfigurationContext();

        context.Model.FindEntityType(typeof(ProjectCollaborator))!.GetProperties().Should().NotContain(property => property.Name == "ProjectId1");
        context.Model.FindEntityType(typeof(ProjectFeedback))!.GetProperties().Should().NotContain(property => property.Name == "ProjectId1");
    }

    private static IMutableEntityType Entity<TEntity>(ModelBuilder modelBuilder)
        => modelBuilder.Model.FindEntityType(typeof(TEntity))!;

    private static FinalizedProjectsConfigContext CreateFinalizedConfigurationContext()
    {
        var options = new DbContextOptionsBuilder<FinalizedProjectsConfigContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new FinalizedProjectsConfigContext(options);
        _ = context.Model;
        return context;
    }

    private sealed class FinalizedProjectsConfigContext(DbContextOptions<FinalizedProjectsConfigContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new ProjectConfiguration().Configure(modelBuilder.Entity<Project>());
            InvokeConfiguration<ProjectCollaborator>(modelBuilder, "GameGuild.Projects.ProjectCollaboratorConfiguration");
            InvokeConfiguration<ProjectFeedback>(modelBuilder, "GameGuild.Projects.ProjectFeedbackConfiguration");
            InvokeConfiguration<ProjectFollower>(modelBuilder, "GameGuild.Projects.ProjectFollowerConfiguration");
            InvokeConfiguration<ProjectJamSubmission>(modelBuilder, "GameGuild.Projects.ProjectJamSubmissionConfiguration");
        }

        private static void InvokeConfiguration<TEntity>(ModelBuilder modelBuilder, string typeName) where TEntity : class
        {
            var type = typeof(Project).Assembly.GetType(typeName, throwOnError: true)!;
            var instance = Activator.CreateInstance(type, nonPublic: true)!;
            ((IEntityTypeConfiguration<TEntity>)instance).Configure(modelBuilder.Entity<TEntity>());
        }
    }
}
