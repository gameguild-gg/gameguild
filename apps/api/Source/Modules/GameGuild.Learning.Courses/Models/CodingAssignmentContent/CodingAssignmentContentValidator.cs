using System.Text.RegularExpressions;
using FluentValidation;

namespace GameGuild.Learning.Courses;

/// <summary>
/// FluentValidation rules for <see cref="CodingAssignmentContent"/> (v1).
/// </summary>
public sealed class CodingAssignmentContentValidator : AbstractValidator<CodingAssignmentContent>
{
    private static readonly HashSet<string> s_validLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "cpp", "c", "sdl-cpp", "raylib-cpp"
    };

    private static readonly HashSet<string> s_validVisibilities = new(StringComparer.Ordinal)
    {
        "Public", "Private"
    };

    private static readonly HashSet<string> s_validParamTypes = new(StringComparer.Ordinal)
    {
        nameof(FunctionParameterType.String),
        nameof(FunctionParameterType.Boolean),
        nameof(FunctionParameterType.Integer),
        nameof(FunctionParameterType.Float),
    };

    // C identifier — bounds C++ name mangling to a portable subset.
    private static readonly Regex s_functionName = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public CodingAssignmentContentValidator()
    {
        RuleFor(x => x.Type)
            .Equal("coding-assignment").WithErrorCode("invalid_type");

        RuleFor(x => x.Version)
            .Equal(1).WithErrorCode("invalid_version");

        RuleFor(x => x.Environment.Language)
            .Must(lang => s_validLanguages.Contains(lang ?? string.Empty))
            .WithMessage("Language must be one of: cpp, c, sdl-cpp, raylib-cpp")
            .WithErrorCode("invalid_language")
            .When(x => x.Environment != null);

        // At least one test in Public ∪ Private
        RuleFor(x => x.Tests)
            .Must(t => (t.Public?.Count ?? 0) + (t.Private?.Count ?? 0) > 0)
            .WithMessage("At least one Public or Private test is required")
            .WithErrorCode("at_least_one_test")
            .When(x => x.Tests != null);

        // Per-test Weight >= 0
        RuleForEach(x => x.Tests.Public)
            .Must(t => t != null && t.Weight >= 0)
            .WithMessage("Test Weight must be non-negative").WithErrorCode("weight_non_negative")
            .When(x => x.Tests?.Public != null);

        RuleForEach(x => x.Tests.Private)
            .Must(t => t != null && t.Weight >= 0)
            .WithMessage("Test Weight must be non-negative").WithErrorCode("weight_non_negative")
            .When(x => x.Tests?.Private != null);

        // Per-file: Visibility ∈ {Public, Private} — reject "Solution" (Must-NOT-Have enforced server-side)
        RuleForEach(x => x.Data.Files.Values)
            .Must(f => f != null && s_validVisibilities.Contains(f.Visibility ?? string.Empty))
            .WithMessage("File Visibility must be Public or Private").WithErrorCode("invalid_visibility_value")
            .When(x => x.Data?.Files != null);

        // Per-file: Private + Modifiable makes no sense (file is hidden from learner but learner can edit?)
        RuleForEach(x => x.Data.Files.Values)
            .Must(f => f == null || !(f.Visibility == "Private" && f.Modifiable))
            .WithMessage("Private files cannot be Modifiable").WithErrorCode("private_file_not_modifiable")
            .When(x => x.Data?.Files != null);

        // FunctionalTest: FunctionName must match C-identifier regex
        RuleForEach(x => x.Tests.Public)
            .Must(IsValidFunctionName)
            .WithMessage("FunctionName must match ^[A-Za-z_][A-Za-z0-9_]*$")
            .WithErrorCode("invalid_function_name")
            .When(x => x.Tests?.Public != null);

        RuleForEach(x => x.Tests.Private)
            .Must(IsValidFunctionName)
            .WithMessage("FunctionName must match ^[A-Za-z_][A-Za-z0-9_]*$")
            .WithErrorCode("invalid_function_name")
            .When(x => x.Tests?.Private != null);

        // FunctionalTest: parameter + return types limited to v1 set (String/Boolean/Integer/Float)
        RuleForEach(x => x.Tests.Public)
            .Must(AreFunctionalParamTypesValid)
            .WithMessage("FunctionalTest parameter type not supported in v1")
            .WithErrorCode("functional_param_type_not_supported_v1")
            .When(x => x.Tests?.Public != null);

        RuleForEach(x => x.Tests.Private)
            .Must(AreFunctionalParamTypesValid)
            .WithMessage("FunctionalTest parameter type not supported in v1")
            .WithErrorCode("functional_param_type_not_supported_v1")
            .When(x => x.Tests?.Private != null);

        // Grading rules
        RuleFor(x => x.Grading.MaxScore)
            .GreaterThan(0).WithMessage("MaxScore must be greater than 0").WithErrorCode("max_score_positive")
            .When(x => x.Grading != null);

        RuleFor(x => x.Grading.PassingScore)
            .GreaterThanOrEqualTo(0).WithMessage("PassingScore must be non-negative").WithErrorCode("passing_score_non_negative")
            .When(x => x.Grading != null);

        RuleFor(x => x.Grading.PassingScore)
            .LessThanOrEqualTo(x => x.Grading.MaxScore)
            .WithMessage("PassingScore must not exceed MaxScore").WithErrorCode("passing_score_within_max")
            .When(x => x.Grading != null);
    }

    private static bool IsValidFunctionName(Test? t)
    {
        if (t is not FunctionalTest f) return true;
        return f.Function != null
            && !string.IsNullOrEmpty(f.Function.FunctionName)
            && s_functionName.IsMatch(f.Function.FunctionName);
    }

    private static bool AreFunctionalParamTypesValid(Test? t)
    {
        if (t is not FunctionalTest f || f.Function == null) return true;
        if (!IsValidParamType(f.Function.ReturnType.Type)) return false;
        foreach (var p in f.Function.Parameters)
        {
            if (p != null && !IsValidParamType(p.Type)) return false;
        }
        return true;
    }

    private static bool IsValidParamType(FunctionParameterType t) =>
        s_validParamTypes.Contains(t.ToString());
}
