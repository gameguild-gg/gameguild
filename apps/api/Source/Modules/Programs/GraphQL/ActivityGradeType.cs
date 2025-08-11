namespace GameGuild.Modules.Programs;

/// <summary>
/// GraphQL type definition for ActivityGrade entity
/// </summary>
public class ActivityGradeType : ObjectType<ActivityGrade> {
  protected override void Configure(IObjectTypeDescriptor<ActivityGrade> descriptor) {
    descriptor.Description("Represents a grade awarded for a content interaction");

    descriptor.Field(ag => ag.Id)
              .Description("Unique identifier for the activity grade");

    descriptor.Field(ag => ag.ContentInteractionId)
              .Description("ID of the content interaction being graded");

    descriptor.Field(ag => ag.GraderProgramUserId)
              .Description("ID of the program user who assigned this grade");

    descriptor.Field(ag => ag.Grade)
              .Description("The grade value (0-100 scale or points based on content)");

    descriptor.Field(ag => ag.Feedback)
              .Description("Written feedback from the grader");

    descriptor.Field(ag => ag.GradingDetails)
              .Description("Detailed grading breakdown stored as JSON (e.g., rubric scores, test results)");

    descriptor.Field(ag => ag.GradedAt)
              .Description("Date when the grade was awarded");

    // Navigation properties
    descriptor.Field(ag => ag.ContentInteraction)
              .Description("The content interaction that was graded");

    descriptor.Field(ag => ag.GraderProgramUser)
              .Description("The program user who assigned this grade");

    // Computed fields
    descriptor.Field("isPassingGrade")
              .Type<NonNullType<BooleanType>>()
              .Description("Whether this grade meets the passing threshold (≥70)")
              .Resolve(context => context.Parent<ActivityGrade>().Grade >= 70);

    descriptor.Field("gradePercentage")
              .Type<NonNullType<StringType>>()
              .Description("Grade formatted as a percentage")
              .Resolve(context => $"{context.Parent<ActivityGrade>().Grade:F1}%");

    descriptor.Field("hasFeedback")
              .Type<NonNullType<BooleanType>>()
              .Description("Whether this grade includes written feedback")
              .Resolve(context => !string.IsNullOrEmpty(context.Parent<ActivityGrade>().Feedback));

    descriptor.Field("hasGradingDetails")
              .Type<NonNullType<BooleanType>>()
              .Description("Whether this grade includes detailed grading breakdown")
              .Resolve(context => !string.IsNullOrEmpty(context.Parent<ActivityGrade>().GradingDetails));

    // Base entity fields
    descriptor.Field(ag => ag.CreatedAt)
              .Description("When the grade was initially created");

    descriptor.Field(ag => ag.UpdatedAt)
              .Description("When the grade was last updated");
  }
}
