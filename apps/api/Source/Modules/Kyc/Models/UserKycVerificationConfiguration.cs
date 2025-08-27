namespace GameGuild.Modules.Kyc.Models;

public class UserKycVerificationConfiguration : IEntityTypeConfiguration<UserKycVerification> {
  public void Configure(EntityTypeBuilder<UserKycVerification> builder) {
    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(ukv => ukv.User).WithMany().HasForeignKey(ukv => ukv.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}
