using FluentAssertions;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class QuestionnaireSchemaCoverageTests
{
    [Fact]
    public void Validate_ReportsNullOversizedAndMalformedQuestions()
    {
        new QuestionnaireSchema("Null", null!).Validate()
            .Should().ContainSingle("Questions are required.");

        var oversized = new QuestionnaireSchema(
            "Large",
            Enumerable.Range(0, 101)
                .Select(index => new QuestionnaireQuestion(
                    $"q{index}", $"Prompt {index}", QuestionnaireQuestionType.FreeText, false))
                .ToArray());
        oversized.Validate().Should().Contain("A questionnaire cannot contain more than 100 questions.");

        var malformed = new QuestionnaireSchema("Malformed", [
            new QuestionnaireQuestion(" ", " ", QuestionnaireQuestionType.FreeText, false),
            new QuestionnaireQuestion("duplicate", "First", QuestionnaireQuestionType.FreeText, false),
            new QuestionnaireQuestion("duplicate", "Second", QuestionnaireQuestionType.FreeText, false),
            new QuestionnaireQuestion("choice", "Choice", QuestionnaireQuestionType.SingleChoice, false,
                [new QuestionnaireOption("one", "One")]),
            new QuestionnaireQuestion("invalid-options", "Invalid", QuestionnaireQuestionType.MultipleChoice, false,
                [new QuestionnaireOption("", "Missing id"), new QuestionnaireOption("valid", "")]),
            new QuestionnaireQuestion("duplicate-options", "Duplicate", QuestionnaireQuestionType.SingleChoice, false,
                [new QuestionnaireOption("same", "One"), new QuestionnaireOption("same", "Two")]),
            new QuestionnaireQuestion("text-options", "Text", QuestionnaireQuestionType.FreeText, false,
                [new QuestionnaireOption("unexpected", "Unexpected")]),
            new QuestionnaireQuestion("conditional", "Conditional", QuestionnaireQuestionType.FreeText, false,
                Condition: new QuestionnaireCondition("future", QuestionnaireConditionOperator.Equals, " "))
        ]);

        var errors = malformed.Validate();
        errors.Should().Contain(error => error.Contains("stable identifier"));
        errors.Should().Contain(error => error.Contains("requires a prompt"));
        errors.Should().Contain(error => error.Contains("duplicated"));
        errors.Should().Contain(error => error.Contains("at least two options"));
        errors.Should().Contain(error => error.Contains("invalid option"));
        errors.Should().Contain(error => error.Contains("duplicated option"));
        errors.Should().Contain(error => error.Contains("cannot define options"));
        errors.Should().Contain(error => error.Contains("earlier question"));
        errors.Should().Contain(error => error.Contains("condition requires a value"));
        Invoking(malformed.EnsureValid).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JsonRoundTrip_CoversValidationOptOutAndMalformedPayloads()
    {
        var invalid = new QuestionnaireSchema("Invalid", [
            new QuestionnaireQuestion("q", " ", QuestionnaireQuestionType.FreeText, false)
        ]);
        var json = invalid.ToJson(ensureValid: false);

        QuestionnaireSchema.FromJson(json, ensureValid: false).Should().BeEquivalentTo(invalid);
        Invoking(() => QuestionnaireSchema.FromJson(json)).Should().Throw<ArgumentException>();
        Invoking(() => QuestionnaireSchema.FromJson(" ")).Should().Throw<ArgumentException>();
        Invoking(() => QuestionnaireSchema.FromJson("not-json")).Should().Throw<ArgumentException>();
        Invoking(() => QuestionnaireSchema.FromJson("null")).Should().Throw<ArgumentException>();
    }

    private static Action Invoking(Action action) => action;
}

public sealed class QuestionnaireResponseCoverageTests
{
    [Fact]
    public void JsonRoundTrip_RejectsNullPayload()
    {
        var response = new QuestionnaireResponse([
            new QuestionnaireAnswer("q", TextValue: "Answer")
        ]);

        QuestionnaireResponse.FromJson(response.ToJson()).Should().BeEquivalentTo(response);
        Invoking(() => QuestionnaireResponse.FromJson("null")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_ReturnsSchemaErrorsBeforeInspectingAnswers()
    {
        var invalidSchema = new QuestionnaireSchema("Invalid", [
            new QuestionnaireQuestion("q", " ", QuestionnaireQuestionType.FreeText, true)
        ]);

        QuestionnaireResponseValidator.Validate(invalidSchema, new QuestionnaireResponse(null!))
            .Should().Contain(error => error.Contains("prompt"));
        Invoking(() => QuestionnaireResponseValidator.Validate(null!, new QuestionnaireResponse([])))
            .Should().Throw<ArgumentNullException>();
        Invoking(() => QuestionnaireResponseValidator.Validate(
                new QuestionnaireSchema("Valid", []), null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_ReportsUnknownRequiredAndTypeSpecificAnswerErrors()
    {
        var schema = new QuestionnaireSchema("Answers", [
            new QuestionnaireQuestion("text", "Text", QuestionnaireQuestionType.FreeText, true),
            new QuestionnaireQuestion("single", "Single", QuestionnaireQuestionType.SingleChoice, true,
                [new QuestionnaireOption("a", "A"), new QuestionnaireOption("b", "B")]),
            new QuestionnaireQuestion("multiple", "Multiple", QuestionnaireQuestionType.MultipleChoice, false,
                [new QuestionnaireOption("a", "A"), new QuestionnaireOption("b", "B")])
        ]);
        var response = new QuestionnaireResponse([
            new QuestionnaireAnswer(" ", TextValue: "ignored"),
            new QuestionnaireAnswer("unknown", TextValue: "unknown"),
            new QuestionnaireAnswer("text", TextValue: null, SelectedOptionIds: ["a"]),
            new QuestionnaireAnswer("single", TextValue: "wrong", SelectedOptionIds: ["a", "b", " "]),
            new QuestionnaireAnswer("single", TextValue: "wrong", SelectedOptionIds: ["a", "invalid"]),
            new QuestionnaireAnswer("multiple", TextValue: "wrong", SelectedOptionIds: ["invalid"])
        ]);

        var errors = QuestionnaireResponseValidator.Validate(schema, response);

        errors.Should().Contain(error => error.Contains("unknown question"));
        errors.Should().Contain(error => error.Contains("accepts text only"));
        errors.Should().Contain(error => error.Contains("option identifiers only"));
        errors.Should().Contain(error => error.Contains("only one option"));
        errors.Should().Contain(error => error.Contains("not an allowed option"));
        Invoking(() => QuestionnaireResponseValidator.EnsureValid(schema, response))
            .Should().Throw<ArgumentException>();

        var missing = QuestionnaireResponseValidator.Validate(schema, new QuestionnaireResponse(null!));
        missing.Should().Contain(error => error.Contains("'text' is required"));
        missing.Should().Contain(error => error.Contains("'single' is required"));
    }

    [Fact]
    public void Conditions_CoverEveryOperatorAndInactivePath()
    {
        var schema = new QuestionnaireSchema("Conditions", [
            new QuestionnaireQuestion("source", "Source", QuestionnaireQuestionType.MultipleChoice, false,
                [new QuestionnaireOption("yes", "Yes"), new QuestionnaireOption("no", "No")]),
            Conditional("equals", QuestionnaireConditionOperator.Equals, "yes"),
            Conditional("not-equals", QuestionnaireConditionOperator.NotEquals, "other"),
            Conditional("includes", QuestionnaireConditionOperator.Includes, "yes"),
            Conditional("inactive", QuestionnaireConditionOperator.Equals, "missing"),
            new QuestionnaireQuestion("unsupported", "Unsupported", QuestionnaireQuestionType.FreeText, true,
                Condition: new QuestionnaireCondition("source", (QuestionnaireConditionOperator)99, "yes"))
        ]);
        var response = new QuestionnaireResponse([
            new QuestionnaireAnswer("source", SelectedOptionIds: ["yes"]),
            new QuestionnaireAnswer("equals", TextValue: "ok"),
            new QuestionnaireAnswer("not-equals", TextValue: "ok"),
            new QuestionnaireAnswer("includes", TextValue: "ok")
        ]);

        QuestionnaireResponseValidator.Validate(schema, response).Should().BeEmpty();
        QuestionnaireResponseValidator.EnsureValid(schema, response);

        var equalsWithMultipleSourceValues = new QuestionnaireResponse([
            new QuestionnaireAnswer("source", SelectedOptionIds: ["yes", "no"])
        ]);
        QuestionnaireResponseValidator.Validate(schema, equalsWithMultipleSourceValues)
            .Should().Contain(error => error.Contains("'not-equals' is required"));

        var textConditionSchema = new QuestionnaireSchema("Text condition", [
            new QuestionnaireQuestion("source", "Source", QuestionnaireQuestionType.FreeText, false),
            new QuestionnaireQuestion("dependent", "Dependent", QuestionnaireQuestionType.FreeText, true,
                Condition: new QuestionnaireCondition("source", QuestionnaireConditionOperator.NotEquals, "no"))
        ]);
        QuestionnaireResponseValidator.Validate(textConditionSchema, new QuestionnaireResponse([
            new QuestionnaireAnswer("source", TextValue: "yes"),
            new QuestionnaireAnswer("dependent", TextValue: "active")
        ])).Should().BeEmpty();

        QuestionnaireResponseValidator.Validate(textConditionSchema, new QuestionnaireResponse([]))
            .Should().BeEmpty();
    }

    private static QuestionnaireQuestion Conditional(
        string id,
        QuestionnaireConditionOperator operation,
        string value) => new(
        id, id, QuestionnaireQuestionType.FreeText, true,
        Condition: new QuestionnaireCondition("source", operation, value));

    private static Action Invoking(Action action) => action;
}
