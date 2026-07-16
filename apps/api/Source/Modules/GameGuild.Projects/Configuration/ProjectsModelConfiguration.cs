using Microsoft.EntityFrameworkCore;

namespace GameGuild.Projects;

public sealed class ProjectsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectCollaboratorConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectFeedbackConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectFollowerConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectJamSubmissionConfiguration());

        modelBuilder.Entity<ProjectStoreProduct>(builder =>
        {
            builder.ToTable("project_store_products");
            builder.HasKey(link => link.Id);
            builder.HasOne(link => link.Project)
                .WithMany()
                .HasForeignKey(link => link.ProjectId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(link => link.Product)
                .WithMany()
                .HasForeignKey(link => link.ProductId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(link => new { link.ProjectId, link.ProductId })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL")
                .HasDatabaseName("IX_project_store_products_active_pair");
            builder.HasIndex(link => link.ProductId);
            builder.HasIndex(link => link.TenantId);
        });

        modelBuilder.Entity<ProjectCategory>(builder =>
        {
            builder.ToTable("project_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Name).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<ProjectMetadata>(builder =>
        {
            builder.ToTable("project_metadata");
            builder.HasKey(metadata => metadata.Id);
            builder.HasOne(metadata => metadata.Project)
                .WithOne(project => project.ProjectMetadata)
                .HasForeignKey<ProjectMetadata>(metadata => metadata.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(metadata => metadata.ProjectId).IsUnique();
        });

        modelBuilder.Entity<ProjectRelease>(builder =>
        {
            builder.ToTable("project_releases");
            builder.HasKey(release => release.Id);
            builder.Property(release => release.Title).IsRequired().HasMaxLength(200);
            builder.Property(release => release.ReleaseVersion).IsRequired().HasMaxLength(50);
            builder.HasOne(release => release.Project)
                .WithMany(project => project.Releases)
                .HasForeignKey(release => release.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(release => new { release.ProjectId, release.ReleaseVersion }).IsUnique();
        });

        modelBuilder.Entity<ProjectVersion>(builder =>
        {
            builder.ToTable("project_versions");
            builder.HasKey(version => version.Id);
            builder.Property(version => version.VersionNumber).IsRequired().HasMaxLength(50);
            builder.HasOne(version => version.Project)
                .WithMany(project => project.Versions)
                .HasForeignKey(version => version.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectTeam>(builder =>
        {
            builder.ToTable("project_teams");
            builder.HasKey(team => team.Id);
            builder.Property(team => team.Role).IsRequired().HasMaxLength(100);
            builder.Property(team => team.Permissions).HasMaxLength(1000);
            builder.Property(team => team.Notes).HasMaxLength(1000);
            builder.HasOne(team => team.Project)
                .WithMany(project => project.Teams)
                .HasForeignKey(team => team.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(team => team.Team)
                .WithMany()
                .HasForeignKey(team => team.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(team => new { team.ProjectId, team.TeamId }).IsUnique();
            builder.HasIndex(team => team.TeamId);
            builder.HasIndex(team => team.AssignedAt);
        });

        modelBuilder.Entity<Team>(builder =>
        {
            builder.ToTable("project_collaboration_teams");
            builder.HasKey(team => team.Id);
            builder.Property(team => team.Name).IsRequired().HasMaxLength(200);
            builder.Property(team => team.Description).HasMaxLength(2000);
            builder.HasIndex(team => team.Name);
        });

        modelBuilder.Entity<TeamMember>(builder =>
        {
            builder.ToTable("project_collaboration_team_members");
            builder.HasKey(member => member.Id);
            builder.Property(member => member.Role).IsRequired().HasMaxLength(100);
            builder.HasOne(member => member.Team)
                .WithMany(team => team.Members)
                .HasForeignKey(member => member.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(member => member.User)
                .WithMany()
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(member => new { member.TeamId, member.UserId }).IsUnique();
            builder.HasIndex(member => member.UserId);
        });

        modelBuilder.Entity<ProjectInvitation>(builder =>
        {
            builder.ToTable("project_invitations");
            builder.HasKey(invitation => invitation.Id);
            builder.Property(invitation => invitation.Token).IsRequired().HasMaxLength(64);
            builder.Property(invitation => invitation.InvitedEmail).HasMaxLength(255);
            builder.Property(invitation => invitation.Role).IsRequired().HasMaxLength(100);
            builder.Property(invitation => invitation.Permissions).IsRequired().HasMaxLength(500);
            builder.HasOne(invitation => invitation.Project)
                .WithMany()
                .HasForeignKey(invitation => invitation.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(invitation => invitation.InvitedUser)
                .WithMany()
                .HasForeignKey(invitation => invitation.InvitedUserId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(invitation => invitation.InvitedByUser)
                .WithMany()
                .HasForeignKey(invitation => invitation.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
