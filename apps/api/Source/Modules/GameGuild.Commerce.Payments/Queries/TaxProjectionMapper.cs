using System.Text.Json;

namespace GameGuild.Commerce.Payments;

internal static class TaxProjectionMapper
{
    public static TaxJurisdictionDto ToJurisdictionDto(TaxJurisdiction jurisdiction, TaxRate? defaultRate)
    {
        var (country, state) = SplitJurisdictionCode(jurisdiction.Code);

        return new TaxJurisdictionDto(
            jurisdiction.Id,
            jurisdiction.Code,
            jurisdiction.Name,
            country,
            state,
            defaultRate?.TaxType.ToString() ?? TaxType.Other.ToString(),
            defaultRate?.Rate ?? 0m,
            jurisdiction.IsActive);
    }

    public static TaxRuleDto ToRuleDto(TaxRule rule)
    {
        return new TaxRuleDto(
            rule.Id,
            rule.TaxJurisdiction.Code,
            rule.DefaultTaxRate?.ProductCategory ?? GetFirstProductCategory(rule.ProductCategories),
            rule.CustomerTypeFilter?.ToString() ?? "Any",
            rule.DefaultTaxRate?.Rate ?? 0m,
            rule.EffectiveFrom ?? rule.DefaultTaxRate?.EffectiveFrom ?? rule.CreatedAt,
            rule.EffectiveTo ?? rule.DefaultTaxRate?.EffectiveTo,
            rule.Description,
            rule.IsActive);
    }

    public static TaxType ParseTaxType(string? value)
    {
        return Enum.TryParse<TaxType>(value, true, out var taxType)
            ? taxType
            : TaxType.Other;
    }

    public static CustomerType? ParseCustomerType(string? value)
    {
        return Enum.TryParse<CustomerType>(value, true, out var customerType)
            ? customerType
            : null;
    }

    public static decimal NormalizeRate(decimal rate)
    {
        if (rate < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Tax rate cannot be negative.");
        }

        if (rate > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Tax rate cannot exceed 100 percent.");
        }

        return rate > 1m ? decimal.Round(rate / 100m, 4) : decimal.Round(rate, 4);
    }

    public static string? SerializeProductCategory(string? productCategory)
    {
        return string.IsNullOrWhiteSpace(productCategory)
            ? null
            : JsonSerializer.Serialize(new[] { productCategory.Trim() });
    }

    private static (string Country, string? State) SplitJurisdictionCode(string code)
    {
        var parts = code.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0
            ? (code, null)
            : (parts[0], parts.Length > 1 ? parts[1] : null);
    }

    private static string? GetFirstProductCategory(string? productCategories)
    {
        if (string.IsNullOrWhiteSpace(productCategories))
        {
            return null;
        }

        try
        {
            var categories = JsonSerializer.Deserialize<List<string>>(productCategories);
            return categories?.FirstOrDefault();
        }
        catch (JsonException)
        {
            return productCategories;
        }
    }
}
