using FluentAssertions;
using FluentValidation;
using System.Text.Json;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

/// <summary>
/// Tests for the v2 CodingAssignmentDefinition schema: polymorphic serialization, validator, and round-trip.
/// </summary>
public class CodingDefinitionTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    // ── Round-trip: valid 4-kind definition ──────────────────────────────

    [Fact]
    public void ValidDefinition_RoundTrips_ThroughJson()
    {
        var def = CreateValidDefinition();

        var json = JsonSerializer.Serialize(def, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<CodingAssignmentDefinition>(json, s_jsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Kind.Should().Be("coding");
        deserialized.Language.Should().Be("cpp");
        deserialized.MaxScore.Should().Be(100);
        deserialized.PassingScore.Should().Be(70);
        deserialized.TestPlan.Should().NotBeNull();
        deserialized.TestPlan!.Cases.Should().HaveCount(4);
    }

    [Fact]
    public void RoundTrip_PreservesDiscriminatorKind()
    {
        var def = CreateValidDefinition();
        var json = JsonSerializer.Serialize(def, s_jsonOptions);
        using var doc = JsonDocument.Parse(json);
        var cases = doc.RootElement.GetProperty("testPlan").GetProperty("cases");

        cases[0].GetProperty("kind").GetString().Should().Be("stdio");
        cases[1].GetProperty("kind").GetString().Should().Be("stdio-file");
        cases[2].GetProperty("kind").GetString().Should().Be("clang-query");
        cases[3].GetProperty("kind").GetString().Should().Be("doctest");
    }

    [Fact]
    public void RoundTrip_PreservesHiddenAndWeight()
    {
        var def = CreateValidDefinition();
        def = def with
        {
            TestPlan = def.TestPlan! with
            {
                Cases =
                [
                    new StdioTestCaseDto { ExpectedStdout = "hello", Weight = 2.5, Hidden = true },
                    new StdioFileTestCaseDto { InFile = "/input.txt", ExpectedOutFile = "/output.txt" },
                    new ClangQueryTestCaseDto { Matcher = "functionDecl()", Expect = "found" },
                    new DoctestTestCaseDto { SourceFiles = ["/tests/test.cpp"] }
                ]
            }
        };

        var json = JsonSerializer.Serialize(def, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<CodingAssignmentDefinition>(json, s_jsonOptions);

        var resultCase = (StdioTestCaseDto)deserialized!.TestPlan!.Cases[0];
        resultCase.Weight.Should().Be(2.5);
        resultCase.Hidden.Should().BeTrue();
    }

    // ── Validator rejects: empty Cases ───────────────────────────────────

    [Fact]
    public void Validator_Rejects_EmptyCases()
    {
        var def = CreateValidDefinition();
        def = def with { TestPlan = def.TestPlan! with { Cases = [] } };

        var errors = Validate(def);
        errors.Should().Contain(e => e.PropertyName.Contains("Cases") || e.ErrorMessage.Contains("at_least_one_case"));
    }

    // ── Validator rejects: bad Language ──────────────────────────────────

    [Fact]
    public void Validator_Rejects_BadLanguage()
    {
        var def = CreateValidDefinition() with { Language = "rust" };

        var errors = Validate(def);
        errors.Should().Contain(e => e.PropertyName == "Language");
    }

    // ── Validator rejects: negative Weight ───────────────────────────────

    [Fact]
    public void Validator_Rejects_NegativeWeight()
    {
        var def = CreateValidDefinition();
        def = def with
        {
            TestPlan = def.TestPlan! with
            {
                Cases = def.TestPlan!.Cases.Select(c =>
                {
                    if (c is StdioTestCaseDto s) return s with { Weight = -1.0 };
                    return c;
                }).ToList()
            }
        };

        var errors = Validate(def);
        errors.Should().Contain(e => e.ErrorMessage.Contains("Weight") || e.PropertyName.Contains("Weight"));
    }

    // ── Validator rejects: stdio missing ExpectedStdout ──────────────────

    [Fact]
    public void Validator_Rejects_Stdio_MissingExpectedStdout()
    {
        var def = CreateValidDefinition();
        def = def with
        {
            TestPlan = def.TestPlan! with
            {
                Cases = def.TestPlan!.Cases.Select(c =>
                {
                    if (c is StdioTestCaseDto s) return s with { ExpectedStdout = "" };
                    return c;
                }).ToList()
            }
        };

        var errors = Validate(def);
        errors.Should().Contain(e => e.ErrorMessage.Contains("ExpectedStdout"));
    }

    // ── Validator rejects: doctest missing SourceFiles ───────────────────

    [Fact]
    public void Validator_Rejects_Doctest_MissingSourceFiles()
    {
        var def = CreateValidDefinition();
        def = def with
        {
            TestPlan = def.TestPlan! with
            {
                Cases = def.TestPlan!.Cases.Select(c =>
                {
                    if (c is DoctestTestCaseDto d) return d with { SourceFiles = [] };
                    return c;
                }).ToList()
            }
        };

        var errors = Validate(def);
        errors.Should().Contain(e => e.ErrorMessage.Contains("SourceFiles"));
    }

    // ── Validator rejects: clang-query missing Matcher ───────────────────

    [Fact]
    public void Validator_Rejects_ClangQuery_MissingMatcher()
    {
        var def = CreateValidDefinition();
        def = def with
        {
            TestPlan = def.TestPlan! with
            {
                Cases = def.TestPlan!.Cases.Select(c =>
                {
                    if (c is ClangQueryTestCaseDto q) return q with { Matcher = "" };
                    return c;
                }).ToList()
            }
        };

        var errors = Validate(def);
        errors.Should().Contain(e => e.ErrorMessage.Contains("Matcher"));
    }

    // ── Helper: valid definition ─────────────────────────────────────────

    private static CodingAssignmentDefinition CreateValidDefinition()
    {
        return new CodingAssignmentDefinition
        {
            Kind = "coding",
            Language = "cpp",
            MaxScore = 100,
            PassingScore = 70,
            TestPlan = new CodingTestPlanDto
            {
                Cases =
                [
                    new StdioTestCaseDto { ExpectedStdout = "hello" },
                    new StdioFileTestCaseDto { InFile = "/input.txt", ExpectedOutFile = "/output.txt" },
                    new ClangQueryTestCaseDto { Matcher = "functionDecl()", Expect = "found" },
                    new DoctestTestCaseDto { SourceFiles = ["/tests/test.cpp"] }
                ]
            }
        };
    }

    // ── Helper: validate ─────────────────────────────────────────────────

    private static List<FluentValidation.Results.ValidationFailure> Validate(CodingAssignmentDefinition def)
    {
        var validator = new CodingAssignmentDefinitionValidator();
        var result = validator.Validate(def);
        return result.Errors;
    }
}
