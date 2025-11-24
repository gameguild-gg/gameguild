using GameGuild.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Authentication.Database.Configurations;

/// <summary>
/// Entity Framework Core configuration for the UserRole entity.
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_role", "gameguild.authentication");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(ur => ur.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ur => ur.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(ur => ur.AssignedBy)
            .HasColumnName("assigned_by");

        builder.Property(ur => ur.AssignedAt)
            .HasColumnName("assigned_at")
            .IsRequired();

        builder.Property(ur => ur.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(ur => ur.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ur => ur.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(ur => ur.IsGlobal)
            .HasColumnName("is_global")
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(ur => ur.Role)
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ur => ur.UserId)
            .HasDatabaseName("idx_user_role_user_id");

        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("idx_user_role_role_id");

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasDatabaseName("idx_user_role_user_id_role_id");

        builder.HasIndex(ur => ur.AssignedBy)
            .HasDatabaseName("idx_user_role_assigned_by");

        builder.HasIndex(ur => ur.ExpiresAt)
            .HasDatabaseName("idx_user_role_expires_at");
    }
}
