namespace GameGuild.Modules.Programs;

/// <summary>
/// GraphQL type definition for ContentInteraction entity
/// </summary>
public class ContentInteractionType : ObjectType<ContentInteraction> {
  protected override void Configure(IObjectTypeDescriptor<ContentInteraction> descriptor) {
    descriptor.Description("Represents a user's interaction with program content");

    descriptor.Field(ci => ci.Id)
              .Description("Unique identifier for the content interaction");

    descriptor.Field(ci => ci.ProgramUserId)
              .Description("ID of the program user who is interacting with the content");

    descriptor.Field(ci => ci.ContentId)
              .Description("ID of the content being interacted with");

    descriptor.Field(ci => ci.Status)
              .Description("Current progress status of the interaction");

    descriptor.Field(ci => ci.SubmissionData)
              .Description("JSON data containing user's submission or response to the content");

    descriptor.Field(ci => ci.CompletionPercentage)
              .Description("Completion percentage for this specific content (0-100)");

    descriptor.Field(ci => ci.TimeSpentMinutes)
              .Description("Time spent on this content in minutes");

    descriptor.Field(ci => ci.FirstAccessedAt)
              .Description("Date when user first accessed this content");

    descriptor.Field(ci => ci.LastAccessedAt)
              .Description("Date when user last accessed this content");

    descriptor.Field(ci => ci.CompletedAt)
              .Description("Date when user completed this content");

    descriptor.Field(ci => ci.SubmittedAt)
              .Description("Date when user submitted their work (for gradeable content). Once submitted, interaction becomes immutable");

    // Navigation properties
    descriptor.Field(ci => ci.ProgramUser)
              .Description("The program user who is interacting with the content");

    descriptor.Field(ci => ci.Content)
              .Description("The program content being interacted with");

    descriptor.Field(ci => ci.ActivityGrades)
              .Description("Activity grades associated with this interaction");

    // Computed fields
    descriptor.Field("isSubmitted")
              .Type<NonNullType<BooleanType>>()
              .Description("Whether this interaction has been submitted and is now immutable")
              .Resolve(context => context.Parent<ContentInteraction>().SubmittedAt.HasValue);

    descriptor.Field("canModify")
              .Type<NonNullType<BooleanType>>()
              .Description("Whether this interaction can still be modified (not submitted)")
              .Resolve(context => !context.Parent<ContentInteraction>().SubmittedAt.HasValue);

    descriptor.Field("durationInMinutes")
              .Type<IntType>()
              .Description("Duration between first and last access in minutes")
              .Resolve(context => {
                var interaction = context.Parent<ContentInteraction>();

                if (interaction is { FirstAccessedAt: not null, LastAccessedAt: not null }) return (int)(interaction.LastAccessedAt.Value - interaction.FirstAccessedAt.Value).TotalMinutes;

                return null;
              }
              );

    // Base entity fields
    descriptor.Field(ci => ci.CreatedAt)
              .Description("When the interaction was created");

    descriptor.Field(ci => ci.UpdatedAt)
              .Description("When the interaction was last updated");
  }
}
