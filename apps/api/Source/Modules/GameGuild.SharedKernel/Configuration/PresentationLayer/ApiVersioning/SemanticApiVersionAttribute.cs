using Asp.Versioning;

namespace GameGuild.Configuration.PresentationLayer.ApiVersioning;

/// <summary>Declares a controller API version using semantic major.minor.patch syntax.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class SemanticApiVersionAttribute(string version) : ApiVersionAttribute(SemanticApiVersionParser.Instance, version) { }

/// <summary>Maps an action to a semantic major.minor.patch API version.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class MapToSemanticApiVersionAttribute(string version) : MapToApiVersionAttribute(SemanticApiVersionParser.Instance, version) { }
