using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Payments.Models;

public class UserFinancialMethodConfiguration : IEntityTypeConfiguration<UserFinancialMethod> {
  public void Configure(EntityTypeBuilder<UserFinancialMethod> builder) {
    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(ufm => ufm.User).WithMany().HasForeignKey(ufm => ufm.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}
