using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameGuild.Modules.Contents;

// Quick test to verify JSON serialization works with AccessLevel enum
public class TestAccessLevel
{
    public AccessLevel Visibility { get; set; }
    public string Name { get; set; } = "";
}

public class JsonTestRunner
{
    public static void Main()
    {
        var test = new TestAccessLevel
        {
            Visibility = AccessLevel.Public,
            Name = "Test"
        };

        var options = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // Serialize
        var json = JsonSerializer.Serialize(test, options);
        Console.WriteLine($"Serialized: {json}");

        // Deserialize
        var result = JsonSerializer.Deserialize<TestAccessLevel>(json, options);
        Console.WriteLine($"Deserialized: {result?.Name} - {result?.Visibility}");

        Console.WriteLine("JSON serialization test completed successfully!");
    }
}
