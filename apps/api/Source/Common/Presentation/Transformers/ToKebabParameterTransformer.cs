using System.Text.RegularExpressions;


namespace GameGuild.Common.Presentation.Transformers;

/// <summary>
/// Transforms route parameters from PascalCase to kebab-case
/// Example: "TenantRoles" becomes "tenant-roles"
/// </summary>
public partial class ToKebabParameterTransformer : IOutboundParameterTransformer {
  [GeneratedRegex("([a-z])([A-Z])")] private static partial Regex KebabCaseGeneratedRegex();

  public string? TransformOutbound(object? value) { return value is not string s ? null : KebabCaseGeneratedRegex().Replace(s, "$1-$2").ToLower(); }
}
