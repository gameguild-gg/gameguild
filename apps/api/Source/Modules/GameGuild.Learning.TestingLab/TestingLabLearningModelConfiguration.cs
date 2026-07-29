using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.TestingLab;

public sealed class TestingLabLearningModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestingLabLearningEvidenceReceipt>(builder =>
        {
            builder.ToTable("testing_lab_learning_evidence_receipts");
            builder.HasKey(receipt => receipt.Id);
            builder.Property(receipt => receipt.Requirement).HasConversion<string>().HasMaxLength(100);
            builder.HasIndex(receipt => receipt.EvidenceId).IsUnique();
            builder.HasIndex(receipt => receipt.RegistrationId).IsUnique();
            builder.HasIndex(receipt => receipt.TenantId);
            builder.HasIndex(receipt => new
            {
                receipt.UserId,
                receipt.CourseId,
                receipt.LearningActivityId
            });
        });
    }
}
