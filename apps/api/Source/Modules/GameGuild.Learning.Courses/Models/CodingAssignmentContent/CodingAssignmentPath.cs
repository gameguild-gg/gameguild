using System.Text.Json;

namespace GameGuild.Learning.Courses;

public static class CodingAssignmentPath
{
    public static string Normalize(string path)
    {
        var relative = path.StartsWith("/home/user/", StringComparison.Ordinal) ? path[11..] : path.StartsWith("/user/", StringComparison.Ordinal) ? path[6..] : path;
        var segments = relative.Split('/');
        if (relative.Length is 0 or > 240 || relative.StartsWith('/') || relative.Contains('\\') || relative.Contains(':') || relative.Any(char.IsControl) || segments.Any(segment => segment is "" or "." or "..") || segments.Any(segment => segment.StartsWith("functional_", StringComparison.Ordinal) && segment.EndsWith("_test.cpp", StringComparison.Ordinal)))
        {
            throw new JsonException("Code workspace path is invalid or reserved.");
        }

        return "/home/user/" + relative;
    }
}
