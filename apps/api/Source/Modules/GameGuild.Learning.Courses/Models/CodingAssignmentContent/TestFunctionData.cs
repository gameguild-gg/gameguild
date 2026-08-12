using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Learning.Courses;

/// <summary>
/// C/C++ function signature invoked by a <see cref="FunctionalTest"/>.
/// Field names are PascalCase on the wire (deliberate divergence from the draft).
/// </summary>
public sealed record TestFunctionData
{
    public required string FunctionName { get; init; }

    public List<FunctionParameterWithName> Parameters { get; init; } = new();

    public required FunctionParameter ReturnType { get; init; }
}

/// <summary>
/// Typed value binding for a function parameter or return slot.
/// <see cref="Content"/> is left as <see cref="JsonElement"/> so string / number / bool payloads round-trip verbatim.
/// </summary>
public record FunctionParameter
{
    public required FunctionParameterType Type { get; init; }

    public required JsonElement Content { get; init; }
}

/// <summary>
/// A <see cref="FunctionParameter"/> with an additional <see cref="Name"/> (used for function arguments).
/// </summary>
public sealed record FunctionParameterWithName : FunctionParameter
{
    public required string Name { get; init; }
}

/// <summary>
/// v1 parameter type set. Array/Dictionary are deferred to v2 and rejected server-side.
/// Serialized as lowercase string values ("string", "boolean", "integer", "float").
/// </summary>
[JsonConverter(typeof(CamelCaseEnumConverter<FunctionParameterType>))]
public enum FunctionParameterType
{
    String,
    Boolean,
    Integer,
    Float,
}

/// <summary>
/// <see cref="JsonStringEnumConverter{TEnum}"/> preset with camelCase naming so enum values serialize
/// as lowercase wire strings.
/// </summary>
internal sealed class CamelCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public CamelCaseEnumConverter() : base(JsonNamingPolicy.CamelCase) { }
}
