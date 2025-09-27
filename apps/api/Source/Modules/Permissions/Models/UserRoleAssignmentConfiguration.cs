namespace GameGuild.Modules.Permissions;

/// <summary> Configuration for UserRoleAssignment entity </summary>
internal class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment> {
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder) {
        builder.HasIndex(x => new { x.UserId, x.Module, x.RoleName }).IsUnique();

        builder.HasOne(x => x.Role).WithMany(x => x.UserRoleAssignments).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.SetNull);
    }
}