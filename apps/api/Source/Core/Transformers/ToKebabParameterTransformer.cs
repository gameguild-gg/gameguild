namespace GameGuild.Core.Transformers;

/// <summary>
/// ASP.NET Core route parameter transformer that converts PascalCase route values to kebab-case URLs.
/// Used for consistent URL formatting across the application.
/// </summary>
public class ToKebabParameterTransformer : IOutboundParameterTransformer {
  private static readonly KebabCaseTransformer Transformer = new();

  public string? TransformOutbound(object? value) {
    return value is not string s ? null : Transformer.Transform(s);
  }
}
