using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Certificates.Models;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate> {
  public void Configure(EntityTypeBuilder<Certificate> builder) {
    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(c => c.Program).WithMany().HasForeignKey(c => c.ProgramId).OnDelete(DeleteBehavior.SetNull);

    // Configure relationship with Product (can't be done with annotations)
    builder.HasOne(c => c.Product).WithMany().HasForeignKey(c => c.ProductId).OnDelete(DeleteBehavior.SetNull);
  }
}
