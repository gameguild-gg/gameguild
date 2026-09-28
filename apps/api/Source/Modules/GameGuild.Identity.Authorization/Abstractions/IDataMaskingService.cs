using System.Text.Json;

namespace GameGuild.Identity.Authorization;

/// <summary>Applies tenant-aware field masking to a successful API response value.</summary>
public interface IDataMaskingService
{
    Task<object?> ApplyAsync(
        string resourceType,
        object value,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken = default);
}

/// <summary>Overrides the response resource type used to look up masking rules.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class DataMaskingResourceTypeAttribute(string resourceType) : Attribute
{
    public string ResourceType { get; } = string.IsNullOrWhiteSpace(resourceType)
        ? throw new ArgumentException("A resource type is required", nameof(resourceType))
        : resourceType;
}
