using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Projects.Models;

/// <summary>
/// Entity configuration for Project
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project> {
  public void Configure(EntityTypeBuilder<Project> builder) {
    // Table configuration
    builder.ToTable("Projects");

    // Properties
    builder.Property(p => p.Title).IsRequired().HasMaxLength(200);

    builder.Property(p => p.Description).HasMaxLength(2000);

    builder.Property(p => p.ShortDescription).HasMaxLength(500);

    builder.Property(p => p.ImageUrl).HasMaxLength(500);

    builder.Property(p => p.WebsiteUrl).HasMaxLength(500);

    builder.Property(p => p.RepositoryUrl).HasMaxLength(500);

    builder.Property(p => p.DownloadUrl).HasMaxLength(500);

    // JSON properties
    builder.Property(p => p.SocialLinks)
           .HasConversion(
             v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
             v => v == null ? null : JsonSerializer.Deserialize<string>(v, (JsonSerializerOptions?)null)
           );

    builder.Property(p => p.Tags)
           .HasConversion(
             v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
             v => v == null ? null : JsonSerializer.Deserialize<string>(v, (JsonSerializerOptions?)null)
           );

    // Relationships
    builder.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);

    builder.HasOne(p => p.CreatedBy).WithMany().HasForeignKey(p => p.CreatedById).OnDelete(DeleteBehavior.SetNull);
    builder.HasMany(p => p.Versions)
           .WithOne(v => v.Project)
           .HasForeignKey(v => v.ProjectId)
           .OnDelete(DeleteBehavior.Cascade);
    builder.HasMany(p => p.Collaborators)
           .WithOne(c => c.Project)
           .HasForeignKey(c => c.ProjectId)
           .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(p => p.Teams)
           .WithOne(t => t.Project)
           .HasForeignKey(t => t.ProjectId)
           .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(p => p.Followers)
           .WithOne(f => f.Project)
           .HasForeignKey(f => f.ProjectId)
           .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(p => p.Feedbacks)
           .WithOne(f => f.Project)
           .HasForeignKey(f => f.ProjectId)
           .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(p => p.JamSubmissions)
           .WithOne(js => js.Project)
           .HasForeignKey(js => js.ProjectId)
           .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(p => p.Tenant).WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.SetNull);

    // Indexes
    builder.HasIndex(p => p.Title);
    builder.HasIndex(p => p.Status);
    builder.HasIndex(p => p.Visibility);
    builder.HasIndex(p => p.CreatedById);
    builder.HasIndex(p => p.CategoryId);
    builder.HasIndex(p => p.TenantId);
    builder.HasIndex(p => p.CreatedAt);
    builder.HasIndex(p => p.UpdatedAt);
    builder.HasIndex(p => new { p.Status, p.Visibility });
    builder.HasIndex(p => new { p.CategoryId, p.Status });
    builder.HasIndex(p => new { p.TenantId, p.Status });
  }
}
