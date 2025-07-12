using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Feedbacks;

public class ProgramRatingConfiguration : IEntityTypeConfiguration<ProgramRating> {
  public void Configure(EntityTypeBuilder<ProgramRating> builder) {
    // Configure relationship with Program (specify navigation property)
    builder.HasOne(pr => pr.Program).WithMany(p => p.ProgramRatings).HasForeignKey(pr => pr.ProgramId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with User (User doesn't have a navigation property collection for ratings)
    builder.HasOne(pr => pr.User).WithMany().HasForeignKey(pr => pr.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}
