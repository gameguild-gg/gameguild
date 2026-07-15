using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Learning.Courses;

public sealed class ContentInteractionEventConfiguration : IEntityTypeConfiguration<ContentInteractionEvent>
{
    public void Configure(EntityTypeBuilder<ContentInteractionEvent> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_content_interaction_events_DurationSeconds_Positive",
                "\"DurationSeconds\" IS NULL OR \"DurationSeconds\" > 0");
            table.HasCheckConstraint(
                "CK_content_interaction_events_PositionSeconds_NonNegative",
                "\"PositionSeconds\" IS NULL OR \"PositionSeconds\" >= 0");
            table.HasCheckConstraint(
                "CK_content_interaction_events_ProgressPercentage_Range",
                "\"ProgressPercentage\" IS NULL OR (\"ProgressPercentage\" >= 0 AND \"ProgressPercentage\" <= 100)");
        });

        builder.HasOne(item => item.Interaction)
            .WithMany(interaction => interaction.Events)
            .HasForeignKey(item => item.InteractionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
