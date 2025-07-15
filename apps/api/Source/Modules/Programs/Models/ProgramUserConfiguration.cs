using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Entity Framework configuration for ProgramUser entity
/// </summary>
public class ProgramUserConfiguration : IEntityTypeConfiguration<ProgramUser> {
  public void Configure(EntityTypeBuilder<ProgramUser> builder) {
    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(pu => pu.Program)
           .WithMany(p => p.ProgramUsers)
           .HasForeignKey(pu => pu.ProgramId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(pu => pu.User).WithMany().HasForeignKey(pu => pu.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}
