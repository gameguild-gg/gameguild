namespace GameGuild.Learning.Courses;

internal static class ProgramContentTree
{
    internal static IReadOnlyCollection<Guid> GetIds(
        Guid rootId,
        IEnumerable<ProgramContent> contents)
    {
        var childrenByParentId = contents
            .Where(content => content.ParentId.HasValue)
            .GroupBy(content => content.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(content => content.Id).ToArray());
        var result = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(rootId);

        while (pending.Count > 0)
        {
            var contentId = pending.Pop();
            if (!result.Add(contentId) || !childrenByParentId.TryGetValue(contentId, out var childIds))
            {
                continue;
            }

            foreach (var childId in childIds)
            {
                pending.Push(childId);
            }
        }

        return result;
    }
}
