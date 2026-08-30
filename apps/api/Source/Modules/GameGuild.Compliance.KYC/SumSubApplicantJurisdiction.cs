using System.Text.Json;
using GameGuild.Economy.Risk;

namespace GameGuild.Compliance.KYC;

public static class SumSubApplicantJurisdiction
{
    public static string? Resolve(JsonElement applicant)
    {
        return Normalize(NestedCountry(applicant, "info"))
            ?? Normalize(DirectCountry(applicant))
            ?? Normalize(NestedCountry(applicant, "fixedInfo"));
    }

    public static string? Normalize(string? value)
        => EconomyJurisdictionCode.NormalizeOptional(value);

    private static string? DirectCountry(JsonElement applicant) =>
        StringProperty(applicant, "country");

    private static string? NestedCountry(JsonElement applicant, string propertyName) =>
        applicant.TryGetProperty(propertyName, out var nested) && nested.ValueKind == JsonValueKind.Object
            ? StringProperty(nested, "country")
            : null;

    private static string? StringProperty(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
