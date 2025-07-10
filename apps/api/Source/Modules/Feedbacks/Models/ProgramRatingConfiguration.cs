using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Feedbacks.Models;

public class ProgramRatingConfiguration : IEntityTypeConfiguration<ProgramRating> {
  public void Configure(EntityTypeBuilder<ProgramRating> builder) {
    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(pr => pr.Program).WithMany().HasForeignKey(pr => pr.ProgramId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(pr => pr.User).WithMany().HasForeignKey(pr => pr.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}
