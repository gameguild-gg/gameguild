namespace GameGuild.Learning.Courses;

internal static class ContentInteractionAttemptSelection
{
  public static IReadOnlyList<ContentInteraction> CurrentPerContent(
    IEnumerable<ContentInteraction> interactions)
  {
    return interactions
      .GroupBy(interaction => interaction.ContentId)
      .Select(group => group
        .OrderBy(interaction => interaction.SubmittedAt.HasValue)
        .ThenByDescending(interaction => interaction.CreatedAt)
        .ThenByDescending(interaction => interaction.Id)
        .First())
      .OrderBy(interaction => interaction.Content.SortOrder)
      .ThenBy(interaction => interaction.Content.Title)
      .ToList();
  }
}
