using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GameGuild.Learning.Courses;

internal static class CodingPublicationMetadataValidator
{
    private static readonly Regex s_identifier = new(@"^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$", RegexOptions.Compiled);

    public static bool IsValid(CodingAssignmentContent content)
    {
        var complete = content.Problem is not null;
        var files = content.Data?.Files;
        var tests = content.Tests?.Public?.Concat(content.Tests.Private ?? []).ToArray();
        if (files is null || tests is null || files.Values.Any(file => file is null) || tests.Any(test => test is null))
        {
            return false;
        }

        if (!ValidIds(files.Values.Select(file => file.Id), complete) || !ValidIds(tests.Select(test => test.Id), complete)
            || tests.OfType<FunctionalTestGroup>().Any(test => test.Cases is null || test.Cases.Any(value => value is null)
                || !ValidIds(test.Cases.Select(value => value.Id), complete)))
        {
            return false;
        }

        if (content.Problem is { } problem && (!Text(problem.Statement, required: true) || problem.Examples is null
            || problem.Examples.Count > 100 || problem.Constraints is null || problem.Constraints.Count > 100
            || problem.Constraints.Any(constraint => !Text(constraint, required: true))
            || problem.Examples.Any(example => example is null || !Text(example.Input) || !Text(example.Output) || !Text(example.Explanation))
            || !ValidIds(problem.Examples.Select(example => example.Id), true)))
        {
            return false;
        }

        if (content.ValidationSolutions is not { } solutions)
        {
            return HasCoherentFamily(content, tests) && WithinBudget(content);
        }

        if (solutions.Count > 100 || (solutions.Count > 0 && !complete) || solutions.Any(solution => solution is null)
            || !ValidIds(solutions.Select(solution => solution.Id), true))
        {
            return false;
        }

        foreach (var solution in solutions)
        {
            if (!Text(solution.Name, required: true) || solution.Name.Length > 256 || solution.Role is not ("reference" or "incorrect")
                || solution.Files is null || solution.Files.Count is 0 or > 100 || solution.Files.Any(file => file is null || !Text(file.Content))
                || !ValidIds(solution.Files.Select(file => file.FileId), true))
            {
                return false;
            }

            foreach (var replacement in solution.Files)
            {
                var authored = files.FirstOrDefault(file => file.Value.Id == replacement.FileId);
                if (authored.Value is not { Encoding: "text", Visibility: "Public", Modifiable: true } || replacement.Path is null)
                {
                    return false;
                }

                try
                {
                    if (CodingAssignmentPath.Normalize(authored.Key) != CodingAssignmentPath.Normalize(replacement.Path))
                    {
                        return false;
                    }
                }
                catch (JsonException)
                {
                    return false;
                }
            }
        }

        return HasCoherentFamily(content, tests) && WithinBudget(content);
    }

    private static bool HasCoherentFamily(CodingAssignmentContent content, Test[] tests)
    {
        if (content.Problem is null)
        {
            return true;
        }

        if (content.ExerciseType == "standard-io")
        {
            return tests.All(test => test is StandardTest);
        }

        var functions = tests.OfType<FunctionalTestGroup>().ToArray();
        return functions.Length == tests.Length && functions.All(test => test.Function is not null)
            && functions.Select(test => JsonSerializer.Serialize(test.Function)).Distinct(StringComparer.Ordinal).Count() <= 1;
    }

    private static bool WithinBudget(CodingAssignmentContent content) =>
        JsonSerializer.SerializeToUtf8Bytes(new { content.Problem, content.ValidationSolutions }).Length <= 10_000_000;

    private static bool ValidIds(IEnumerable<string?> ids, bool required)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return ids.All(id => id is null ? !required : s_identifier.IsMatch(id) && seen.Add(id));
    }

    private static bool Text(string? value, bool required = false) => value is not null
        && (!required || !string.IsNullOrWhiteSpace(value)) && Encoding.UTF8.GetByteCount(value) <= 2_000_000;
}
