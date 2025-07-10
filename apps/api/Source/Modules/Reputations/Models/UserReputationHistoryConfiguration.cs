using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Reputations.Models;

public class UserReputationHistoryConfiguration : IEntityTypeConfiguration<UserReputationHistory> {
  public void Configure(EntityTypeBuilder<UserReputationHistory> builder) {
    // Check constraint for polymorphic relationship (can't be done with annotations)
    builder.ToTable(
      "UserReputationHistory",
      t => t.HasCheckConstraint(
        "CK_UserReputationHistory_UserOrUserTenant",
        "(\"UserId\" IS NOT NULL AND \"UserTenantId\" IS NULL) OR (\"UserId\" IS NULL AND \"UserTenantId\" IS NOT NULL)"
      )
    );

    // Configure an optional relationship with User (can't be done with annotations)
    builder.HasOne(urh => urh.User).WithMany().HasForeignKey(urh => urh.UserId).OnDelete(DeleteBehavior.SetNull);

    // Configure an optional relationship with UserTenant (can't be done with annotations)
    builder.HasOne(urh => urh.TenantPermission)
           .WithMany()
           .HasForeignKey(urh => urh.TenantPermissionId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a relationship with ReputationAction (can't be done with annotations)
    builder.HasOne(urh => urh.ReputationAction)
           .WithMany(ra => ra.ReputationHistory)
           .HasForeignKey(urh => urh.ReputationActionId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a relationship with TriggeredByUser (can't be done with annotations)
    builder.HasOne(urh => urh.TriggeredByUser)
           .WithMany()
           .HasForeignKey(urh => urh.TriggeredByUserId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a relationship with PreviousLevel (can't be done with annotations)
    builder.HasOne(urh => urh.PreviousLevel)
           .WithMany()
           .HasForeignKey(urh => urh.PreviousLevelId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a relationship with NewLevel (can't be done with annotations)
    builder.HasOne(urh => urh.NewLevel)
           .WithMany()
           .HasForeignKey(urh => urh.NewLevelId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a polymorphic relationship with RelatedResource (can't be done with annotations)
    builder.HasOne(urh => urh.RelatedResource)
           .WithMany()
           .HasForeignKey("RelatedResourceId")
           .OnDelete(DeleteBehavior.SetNull);
  }
}
