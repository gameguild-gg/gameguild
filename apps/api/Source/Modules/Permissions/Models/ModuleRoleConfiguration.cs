namespace GameGuild.Modules.Permissions;

/// <summary> Configuration for ModuleRole entity </summary>
public class ModuleRoleConfiguration : IEntityTypeConfiguration<ModuleRole> {
    public void Configure(EntityTypeBuilder<ModuleRole> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasIndex(x => new { x.Name, x.Module, x.TenantId }).IsUnique();

        builder.HasMany(x => x.UserRoleAssignments).WithOne(x => x.Role).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.SetNull);
    }
}