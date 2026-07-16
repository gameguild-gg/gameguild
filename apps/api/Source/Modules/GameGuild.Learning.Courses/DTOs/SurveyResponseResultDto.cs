using System.Text.Json;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Survey result projection. It deliberately excludes learner and enrollment identifiers.
/// </summary>
public sealed record SurveyResponseResultDto(Guid ResponseId, DateTime? SubmittedAt, IReadOnlyDictionary<string, JsonElement> Answers)
{
    public static SurveyResponseResultDto FromInteraction(ContentInteraction interaction)
    {
        var response = ActivityResponseContract.Parse(
            ProgramContentType.Survey,
            interaction.SubmissionData ?? throw new InvalidOperationException("Survey submission data is missing."),
            null) as SurveyActivityResponse
            ?? throw new InvalidOperationException("Survey submission data is invalid.");

        return new SurveyResponseResultDto(interaction.Id, interaction.SubmittedAt, response.Answers);
    }
}
