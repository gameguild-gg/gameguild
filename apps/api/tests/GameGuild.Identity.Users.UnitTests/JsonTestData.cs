using System.Text.Json;
using GameGuild;

namespace GameGuild.Identity.Users.UnitTests;

public static class JsonTestData
{
    public static Dictionary<string, JsonElement> JsonMap(IReadOnlyDictionary<string, object?>? source)
    {
        return JsonValueDictionary.ToJsonElements(source);
    }
}
