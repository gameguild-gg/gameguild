using Microsoft.AspNetCore.Routing;

namespace GameGuild.Core.Transformers;

/// <summary>
/// ASP.NET Core route parameter transformer that converts PascalCase route values to snake_case URLs.
/// Used for consistent URL formatting across the application.
/// </summary>
public class ToSnakeParameterTransformer : IOutboundParameterTransformer {
    private static readonly SnakeCaseTransformer Transformer = new();

    public string? TransformOutbound(object? value) {
        return value is not string s ? null : Transformer.Transform(s);
    }
}
