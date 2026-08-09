using FluentValidation;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// FluentValidation rules for CodingAssignmentDefinition.
/// </summary>
public sealed class CodingAssignmentDefinitionValidator : AbstractValidator<CodingAssignmentDefinition>
{
    private static readonly HashSet<string> s_validLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "cpp", "c", "sdl-cpp", "raylib-cpp"
    };

    public CodingAssignmentDefinitionValidator()
    {
        RuleFor(x => x.Language)
            .Must(lang => s_validLanguages.Contains(lang))
            .WithMessage("Language must be one of: cpp, c, sdl-cpp, raylib-cpp")
            .WithErrorCode("invalid_language");

        RuleFor(x => x.MaxScore)
            .GreaterThan(0).WithMessage("MaxScore must be greater than 0").WithErrorCode("max_score_positive");

        RuleFor(x => x.PassingScore)
            .GreaterThanOrEqualTo(0).WithMessage("PassingScore must be non-negative").WithErrorCode("passing_score_non_negative");

        RuleFor(x => x.PassingScore)
            .LessThanOrEqualTo(x => x.MaxScore)
            .WithMessage("PassingScore must not exceed MaxScore").WithErrorCode("passing_score_within_max");

        RuleFor(x => x.TestPlan)
            .NotNull().WithErrorCode("test_plan_required");

        RuleFor(x => x.TestPlan!.Cases)
            .Must(cases => cases is { Count: > 0 })
            .WithMessage("Test plan must contain at least one case").WithErrorCode("at_least_one_case")
            .When(x => x.TestPlan != null);

        // Per-case: Weight >= 0 when present
        RuleForEach(x => x.TestPlan!.Cases)
            .Must(c => c.Weight == null || c.Weight >= 0)
            .WithMessage("Case Weight must be non-negative when specified").WithErrorCode("weight_non_negative")
            .When(x => x.TestPlan != null);

        // Per-kind rules
        When(x => x.TestPlan != null, () =>
        {
            RuleForEach(x => x.TestPlan!.Cases)
                .Must(c => c is not StdioTestCaseDto s || !string.IsNullOrEmpty(s.ExpectedStdout))
                .WithMessage("Stdio test case requires non-empty ExpectedStdout").WithErrorCode("stdio_expected_stdout_required");

            RuleForEach(x => x.TestPlan!.Cases)
                .Must(c => c is not StdioFileTestCaseDto sf || (!string.IsNullOrEmpty(sf.InFile) && !string.IsNullOrEmpty(sf.ExpectedOutFile)))
                .WithMessage("StdioFile test case requires InFile and ExpectedOutFile").WithErrorCode("stdio_file_fields_required");

            RuleForEach(x => x.TestPlan!.Cases)
                .Must(c => c is not ClangQueryTestCaseDto q || (!string.IsNullOrEmpty(q.Matcher) && !string.IsNullOrEmpty(q.Expect)))
                .WithMessage("ClangQuery test case requires Matcher and Expect").WithErrorCode("clang_query_fields_required");

            RuleForEach(x => x.TestPlan!.Cases)
                .Must(c => c is not DoctestTestCaseDto d || d.SourceFiles is { Length: > 0 })
                .WithMessage("Doctest test case requires non-empty SourceFiles").WithErrorCode("doctest_source_files_required");
        });
    }
}
