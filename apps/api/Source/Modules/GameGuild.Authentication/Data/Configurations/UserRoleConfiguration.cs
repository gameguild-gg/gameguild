using GameGuild.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Authentication.Data.Configurations;

/// <summary>
///     Entity Type Configuration for UserRole
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("user_role", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        // Configure UserId property
        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // Configure RoleId property
        builder.Property(x => x.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        // Configure AssignedBy property
        builder.Property(x => x.AssignedBy)
            .HasColumnName("assigned_by")
            .IsRequired(false);

        // Configure AssignedAt property
        builder.Property(x => x.AssignedAt)
            .HasColumnName("assigned_at")
            .IsRequired();

        // Configure ExpiresAt property
        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired(false);

        // Configure timestamps
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Configure relationship with Role
        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_user_role_user_id");

        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("idx_user_role_role_id");

        builder.HasIndex(x => new { x.UserId, x.RoleId })
            .HasDatabaseName("idx_user_role_user_id_role_id")
            .IsUnique();

        builder.HasIndex(x => x.AssignedBy)
            .HasDatabaseName("idx_user_role_assigned_by");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_user_role_expires_at");
    }
}
