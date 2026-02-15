using FluentAssertions;
using Xunit;

namespace GameGuild.SharedKernel.UnitTests;

public class ValidationErrorTests
{
    [Fact]
    public void AggregateValidationError_ShouldWrapErrors()
    {
        var errors = new[] { Error.Validation("a", "err a"), Error.Validation("b", "err b") };
        var agg = new AggregateValidationError(errors);

        agg.Code.Should().Be("Validation.General");
        agg.Type.Should().Be(ErrorType.Validation);
        agg.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void FromResults_ShouldCollectOnlyFailures()
    {
        var results = new[]
        {
            Result.Success(),
            Result.Failure(Error.Validation("a", "err")),
            Result.Success(),
            Result.Failure(Error.Validation("b", "err")),
        };

        var agg = AggregateValidationError.FromResults(results);
        agg.Errors.Should().HaveCount(2);
        agg.Errors[0].Code.Should().Be("a");
        agg.Errors[1].Code.Should().Be("b");
    }

    [Fact]
    public void FromResults_ShouldReturnEmpty_WhenAllSucceed()
    {
        var results = new[] { Result.Success(), Result.Success() };
        var agg = AggregateValidationError.FromResults(results);
        agg.Errors.Should().BeEmpty();
    }
}
