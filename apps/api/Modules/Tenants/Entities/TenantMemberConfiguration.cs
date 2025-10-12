namespace GameGuild.Modules.Tenants;

/// <summary>
///     Entity Framework configuration for the TenantMember entity
/// </summary>
public class TenantMemberConfiguration : IEntityTypeConfiguration<TenantMember>
{
    public void Configure(EntityTypeBuilder<TenantMember> builder)
    {
        // Table configuration
        builder.ToTable("tenant_members");

        // Primary key
        builder.HasKey(tm => tm.Id);

        // Properties
        builder.Property(tm => tm.UserId)
            .IsRequired();

        builder.Property(tm => tm.TenantId)
            .IsRequired();

        builder.Property(tm => tm.Role)
            .HasMaxLength(100);

        builder.Property(tm => tm.IsActive)
            .HasDefaultValue(true);

        builder.Property(tm => tm.JoinedAt)
            .IsRequired();

        builder.Property(tm => tm.LeaveReason)
            .HasMaxLength(500);

        builder.Property(tm => tm.MemberSettings)
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(tm => new { tm.UserId, tm.TenantId })
            .IsUnique()
            .HasDatabaseName("ix_tenant_members_user_tenant");

        builder.HasIndex(tm => new { tm.TenantId, tm.IsActive })
            .HasDatabaseName("ix_tenant_members_tenant_active");

        builder.HasIndex(tm => tm.JoinedAt)
            .HasDatabaseName("ix_tenant_members_joined_at");

        // Relationships
        builder.HasOne(tm => tm.Tenant)
            .WithMany()
            .HasForeignKey(tm => tm.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        builder.HasQueryFilter(tm => !tm.IsDeleted);
    }
}
