using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace GameGuild;

/// <summary>
///     Transforms route parameters from PascalCase to kebab-case
///     Example: "SubscriptionPlans" becomes "subscription-plans"
/// </summary>
public sealed class ToKebabParameterTransformer : IOutboundParameterTransformer
{
    private static readonly Regex CamelCaseRegex = new Regex(@"([a-z0-9])([A-Z])", RegexOptions.Compiled);

    public string? TransformOutbound(object? value)
    {
        if (value is not string stringValue || string.IsNullOrEmpty(stringValue)) return null;

        // Convert PascalCase to kebab-case
        return CamelCaseRegex.Replace(stringValue, "$1-$2").ToLowerInvariant();
    }
}
