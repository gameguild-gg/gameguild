using System.Text.Json;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Contracts;

namespace GameGuild.Learning.Assessments.QuizAdapter;

public sealed class QuizItemProjector : IAssessmentItemProjector
{
    public string Key => QuizAdapterContracts.ProjectorKey;
    public string Version => QuizAdapterContracts.Version;
    public string ContentType => QuizAdapterContracts.ContentType;

    public JsonElement Project(string itemId, JsonElement authoringItem)
    {
        if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID is required.", nameof(itemId));
        QuizAuthoringEntryValidator.Validate(authoringItem);

        var itemType = authoringItem.GetProperty("type").GetString();
        if (itemType is null) throw new JsonException("Quiz entry type is required.");

        var maxScore = authoringItem.TryGetProperty("points", out var points)
            ? ScoreValue.Parse(points.ValueKind == JsonValueKind.String
                ? points.GetString()!
                : throw new JsonException("Quiz points must be a canonical JSON string."))
            : ScoreValue.Parse("00000001.0000");

        return JsonSerializer.SerializeToElement(new
        {
            schemaVersion = 1,
            itemId,
            itemType,
            maxScore,
            source = new { contentType = ContentType, itemId },
            authoringEntry = authoringItem.Clone(),
        });
    }
}
