using Microsoft.EntityFrameworkCore;

namespace GameGuild.LaunchPad;

public sealed class LaunchPadModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LaunchPlan>(builder =>
        {
            builder.ToTable("launch_plans");
            builder.HasKey(plan => plan.Id);
            builder.Property(plan => plan.Name).IsRequired().HasMaxLength(200);
            builder.Property(plan => plan.Positioning).HasMaxLength(1000);
            builder.Property(plan => plan.Channels).HasColumnType("text[]");
            builder.HasIndex(plan => plan.ProjectId)
                .IsUnique()
                .HasDatabaseName("IX_launch_plans_ProjectId")
                .HasFilter("\"DeletedAt\" IS NULL AND \"LaunchPadEventId\" IS NULL");
            builder.HasIndex(plan => plan.LaunchPadApplicationId)
                .IsUnique()
                .HasFilter("\"LaunchPadApplicationId\" IS NOT NULL AND \"DeletedAt\" IS NULL");
            builder.HasOne(plan => plan.Project)
                .WithMany()
                .HasForeignKey(plan => plan.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(plan => plan.ChecklistItems)
                .WithOne(item => item.LaunchPlan)
                .HasForeignKey(item => item.LaunchPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(plan => plan.LaunchPadEvent)
                .WithMany()
                .HasForeignKey(plan => plan.LaunchPadEventId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(plan => plan.LaunchPadApplication)
                .WithOne()
                .HasForeignKey<LaunchPlan>(plan => plan.LaunchPadApplicationId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(plan => plan.ProjectVersion)
                .WithMany()
                .HasForeignKey(plan => plan.ProjectVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LaunchChecklistItem>(builder =>
        {
            builder.ToTable("launch_checklist_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Title).IsRequired().HasMaxLength(200);
            builder.Property(item => item.Category).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<LaunchPadEvent>(builder =>
        {
            builder.ToTable("launch_pad_events");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Name).IsRequired().HasMaxLength(200);
            builder.Property(entity => entity.Description).HasMaxLength(2000);
            builder.HasIndex(entity => new { entity.TenantId, entity.Status, entity.StartsAt });
        });

        modelBuilder.Entity<LaunchPadApplication>(builder =>
        {
            builder.ToTable("launch_pad_applications");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Pitch).HasMaxLength(2000);
            builder.Property(entity => entity.SubmittedAssetReferenceIdsJson).HasMaxLength(10000);
            builder.HasIndex(entity => new { entity.LaunchPadEventId, entity.ProjectId }).IsUnique();
            builder.HasOne(entity => entity.LaunchPadEvent).WithMany(entity => entity.Applications)
                .HasForeignKey(entity => entity.LaunchPadEventId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(entity => entity.Project).WithMany()
                .HasForeignKey(entity => entity.ProjectId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(entity => entity.ProjectVersion).WithMany()
                .HasForeignKey(entity => entity.ProjectVersionId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(entity => entity.SubmittedByUser).WithMany()
                .HasForeignKey(entity => entity.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LaunchPadParticipantSlot>(builder =>
        {
            builder.ToTable("launch_pad_participant_slots");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Name).IsRequired().HasMaxLength(120);
            builder.HasOne(entity => entity.LaunchPadEvent).WithMany(entity => entity.Slots)
                .HasForeignKey(entity => entity.LaunchPadEventId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LaunchPadParticipantRegistration>(builder =>
        {
            builder.ToTable("launch_pad_participant_registrations");
            builder.HasKey(entity => entity.Id);
            builder.HasIndex(entity => new { entity.LaunchPadParticipantSlotId, entity.UserId }).IsUnique();
            builder.HasOne(entity => entity.LaunchPadParticipantSlot).WithMany(entity => entity.Registrations)
                .HasForeignKey(entity => entity.LaunchPadParticipantSlotId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(entity => entity.User).WithMany()
                .HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
