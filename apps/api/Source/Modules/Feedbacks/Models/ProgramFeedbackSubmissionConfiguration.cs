using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Feedbacks;

public class ProgramFeedbackSubmissionConfiguration : IEntityTypeConfiguration<ProgramFeedbackSubmission> {
  public void Configure(EntityTypeBuilder<ProgramFeedbackSubmission> builder) {
    // Configure relationship with Program (specify navigation property)
    builder.HasOne(pfs => pfs.Program).WithMany(p => p.FeedbackSubmissions).HasForeignKey(pfs => pfs.ProgramId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with User (User doesn't have a navigation property collection for feedbacks)
    builder.HasOne(pfs => pfs.User).WithMany().HasForeignKey(pfs => pfs.UserId).OnDelete(DeleteBehavior.Cascade);

    // Configure optional relationship with Product (can't be done with annotations)
    builder.HasOne(pfs => pfs.Product).WithMany().HasForeignKey(pfs => pfs.ProductId).OnDelete(DeleteBehavior.SetNull);

    // Configure relationship with ProgramUser (specify navigation property)
    builder.HasOne(pfs => pfs.ProgramUser)
           .WithMany(pu => pu.FeedbackSubmissions)
           .HasForeignKey(pfs => pfs.ProgramUserId)
           .OnDelete(DeleteBehavior.Cascade);
  }
}
