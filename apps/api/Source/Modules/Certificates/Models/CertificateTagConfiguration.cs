using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Certificates;

public class CertificateTagConfiguration : IEntityTypeConfiguration<CertificateTag> {
  public void Configure(EntityTypeBuilder<CertificateTag> builder) {
    // Configure relationship with Certificate (can't be done with annotations)
    builder.HasOne(ct => ct.Certificate)
           .WithMany()
           .HasForeignKey(ct => ct.CertificateId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with Tag (can't be done with annotations)
    builder.HasOne(ct => ct.Tag).WithMany().HasForeignKey(ct => ct.TagId).OnDelete(DeleteBehavior.Cascade);
  }
}
