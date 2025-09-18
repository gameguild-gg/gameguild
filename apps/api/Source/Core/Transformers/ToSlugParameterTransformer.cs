using Microsoft.AspNetCore.Routing;

namespace GameGuild.Core.Transformers;

/// <summary>
/// ASP.NET Core route parameter transformer that converts PascalCase route values to slug-case URLs.
/// Used for consistent URL formatting across the application.
/// </summary>
public class ToSlugParameterTransformer : IOutboundParameterTransformer {
    private static readonly SlugCaseTransformer Transformer = new();

    public string? TransformOutbound(object? value) {
        return value is not string s ? null : Transformer.Transform(s);
    }
}
