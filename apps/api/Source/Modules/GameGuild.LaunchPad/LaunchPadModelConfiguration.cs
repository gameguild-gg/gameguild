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
            builder.HasOne(plan => plan.Project)
                .WithMany()
                .HasForeignKey(plan => plan.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(plan => plan.ChecklistItems)
                .WithOne(item => item.LaunchPlan)
                .HasForeignKey(item => item.LaunchPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LaunchChecklistItem>(builder =>
        {
            builder.ToTable("launch_checklist_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Title).IsRequired().HasMaxLength(200);
            builder.Property(item => item.Category).IsRequired().HasMaxLength(100);
        });
    }
}
