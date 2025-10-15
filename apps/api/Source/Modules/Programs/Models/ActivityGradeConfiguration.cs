namespace GameGuild.Modules.Programs;

/// <summary>
/// Entity Framework configuration for ActivityGrade entity
/// </summary>
public class ActivityGradeConfiguration : IEntityTypeConfiguration<ActivityGrade> {
  public void Configure(EntityTypeBuilder<ActivityGrade> builder) {
    // Configure relationship with ContentInteraction (can't be done with annotations)
    builder.HasOne(ag => ag.ContentInteraction)
           .WithMany(ci => ci.ActivityGrades)
           .HasForeignKey(ag => ag.ContentInteractionId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with GraderProgramUser (specify the navigation property)
    builder.HasOne(ag => ag.GraderProgramUser)
           .WithMany(pu => pu.GivenGrades)
           .HasForeignKey(ag => ag.GraderProgramUserId)
           .OnDelete(DeleteBehavior.Restrict);
  }
}
