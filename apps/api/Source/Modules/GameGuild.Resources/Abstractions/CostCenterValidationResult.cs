namespace GameGuild.Resources;

public sealed record CostCenterValidationResult(bool IsValid, string Status, string? Message = null)
{
    public static CostCenterValidationResult Validated() => new(true, "Validated");

    public static CostCenterValidationResult Invalid(string message) => new(false, "Invalid", message);
}
