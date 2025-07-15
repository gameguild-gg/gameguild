namespace GameGuild.Common;

/// <summary>
/// Transforms route parameters from PascalCase to kebab-case
/// Example: "TenantRoles" becomes "tenant-roles"
/// </summary>
public partial class ToKebabParameterTransformer : IOutboundParameterTransformer {
  private static readonly KebabCaseTransformer Transformer = new();

  public string? TransformOutbound(object? value) { return value is not string s ? null : Transformer.Transform(s); }
}
