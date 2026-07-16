namespace GameGuild.Learning.Courses;

/// <summary>Reflection response projection. Learner views deliberately omit respondent identity.</summary>
public sealed record ReflectionResponseResultDto(
    Guid ResponseId,
    DateTime? SubmittedAt,
    string Body,
    Guid? RespondentUserId = null)
{
    public static ReflectionResponseResultDto FromInteraction(ContentInteraction interaction, bool includeRespondentIdentity = false)
    {
        var response = ActivityResponseContract.Parse(
            ProgramContentType.Reflection,
            interaction.SubmissionData ?? throw new InvalidOperationException("Reflection submission data is missing."),
            null) as ReflectionActivityResponse
            ?? throw new InvalidOperationException("Reflection submission data is invalid.");

        return new ReflectionResponseResultDto(
            interaction.Id,
            interaction.SubmittedAt,
            response.Body,
            includeRespondentIdentity ? interaction.UserId : null);
    }
}
