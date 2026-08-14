using Microsoft.EntityFrameworkCore;

namespace GameGuild.Teams;

public sealed class TeamsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(builder =>
        {
            builder.ToTable("project_collaboration_teams");
            builder.HasKey(team => team.Id);
            builder.Property(team => team.Name).IsRequired().HasMaxLength(200);
            builder.Property(team => team.Slug).IsRequired().HasMaxLength(200);
            builder.Property(team => team.Description).HasMaxLength(2000);
            builder.Property(team => team.Visibility).HasConversion<string>().HasMaxLength(30);
            builder.Property(team => team.Status).HasConversion<string>().HasMaxLength(30);
            builder.HasIndex(team => new { team.TenantId, team.Slug }).IsUnique();
        });

        modelBuilder.Entity<TeamMember>(builder =>
        {
            builder.ToTable("project_collaboration_team_members");
            builder.HasKey(member => member.Id);
            builder.Property(member => member.Authority).HasConversion<string>().HasMaxLength(30);
            builder.Property(member => member.ProfessionalTitle).HasMaxLength(150);
            builder.HasOne(member => member.Team).WithMany(team => team.Members)
                .HasForeignKey(member => member.TeamId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(member => member.User).WithMany().HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(member => new { member.TeamId, member.UserId }).IsUnique();
            builder.HasIndex(member => member.UserId);
        });

        modelBuilder.Entity<TeamInvitation>(builder =>
        {
            builder.ToTable("team_invitations");
            builder.HasKey(invitation => invitation.Id);
            builder.Property(invitation => invitation.TokenHash).IsRequired().HasMaxLength(64);
            builder.Property(invitation => invitation.InvitedEmail).HasMaxLength(255);
            builder.Property(invitation => invitation.Authority).HasConversion<string>().HasMaxLength(30);
            builder.HasOne(invitation => invitation.Team).WithMany(team => team.Invitations)
                .HasForeignKey(invitation => invitation.TeamId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(invitation => invitation.TokenHash).IsUnique();
            builder.HasIndex(invitation => new { invitation.TeamId, invitation.InvitedEmail });
        });
    }
}
