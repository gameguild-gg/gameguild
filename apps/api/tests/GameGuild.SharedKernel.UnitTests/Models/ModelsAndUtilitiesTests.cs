using FluentAssertions;

namespace GameGuild.SharedKernel.UnitTests.Models;

public class BulkOperationResponseTests
{
    [Fact]
    public void IsComplete_WhenNoFailures_ShouldBeTrue()
    {
        var response = new BulkOperationResponse
        {
            TotalRequested = 10,
            SuccessfulOperations = 10,
            FailedOperations = 0
        };
        response.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void IsComplete_WhenFailures_ShouldBeFalse()
    {
        var response = new BulkOperationResponse
        {
            TotalRequested = 10,
            SuccessfulOperations = 8,
            FailedOperations = 2
        };
        response.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void SuccessRate_AllSuccessful_ShouldBeOne()
    {
        var response = new BulkOperationResponse
        {
            TotalRequested = 10,
            SuccessfulOperations = 10
        };
        response.SuccessRate.Should().Be(1.0);
    }

    [Fact]
    public void SuccessRate_NoneRequested_ShouldBeZero()
    {
        var response = new BulkOperationResponse
        {
            TotalRequested = 0,
            SuccessfulOperations = 0
        };
        response.SuccessRate.Should().Be(0);
    }

    [Fact]
    public void SuccessRate_HalfSuccess_ShouldBeHalf()
    {
        var response = new BulkOperationResponse
        {
            TotalRequested = 10,
            SuccessfulOperations = 5
        };
        response.SuccessRate.Should().BeApproximately(0.5, 0.01);
    }

    [Fact]
    public void Errors_ShouldDefaultToEmpty()
    {
        var response = new BulkOperationResponse();
        response.Errors.Should().BeEmpty();
    }
}

public class RfcUrlsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, "9110#section-15.5.1")]
    [InlineData(ErrorType.Problem, "9110#section-15.5.1")]
    [InlineData(ErrorType.NotFound, "9110#section-15.5.5")]
    [InlineData(ErrorType.Conflict, "9110#section-15.5.10")]
    [InlineData(ErrorType.Unauthorized, "11.6.1")]
    [InlineData(ErrorType.Forbidden, "9110#section-15.5.4")]
    public void ForErrorType_ShouldReturnCorrectUrl(ErrorType errorType, string expectedFragment)
    {
        var url = RfcUrls.ForErrorType(errorType);
        url.Should().Contain(expectedFragment);
    }

    [Fact]
    public void ForErrorType_None_ShouldReturnEmpty()
    {
        RfcUrls.ForErrorType(ErrorType.None).Should().BeEmpty();
    }

    [Fact]
    public void Constants_ShouldBeRfcEditorUrls()
    {
        RfcUrls.BadRequest.Should().StartWith("https://www.rfc-editor.org");
        RfcUrls.NotFound.Should().StartWith("https://www.rfc-editor.org");
        RfcUrls.InternalServerError.Should().StartWith("https://www.rfc-editor.org");
        RfcUrls.Unauthorized.Should().StartWith("https://www.rfc-editor.org");
        RfcUrls.Forbidden.Should().StartWith("https://www.rfc-editor.org");
        RfcUrls.Conflict.Should().StartWith("https://www.rfc-editor.org");
    }
}

public class CustomResultsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Problem, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    public void GetStatusCode_ShouldMapCorrectly(ErrorType errorType, int expectedStatus)
    {
        CustomResults.GetStatusCode(errorType).Should().Be(expectedStatus);
    }

    [Fact]
    public void GetStatusCode_None_ShouldThrow()
    {
        var act = () => CustomResults.GetStatusCode(ErrorType.None);
        act.Should().Throw<InvalidOperationException>();
    }
}

public class ProblemDetailsMapperTests
{
    [Fact]
    public void ToProblemDetails_ValidationError_ShouldSetCorrectStatus()
    {
        var error = Error.Validation("Test.Validation", "Validation failed");
        var pd = ProblemDetailsMapper.ToProblemDetails(error);

        pd.Status.Should().Be(400);
        pd.Title.Should().Be("Test.Validation");
        pd.Detail.Should().Be("Validation failed");
    }

    [Fact]
    public void ToProblemDetails_NotFoundError_ShouldSetStatus404()
    {
        var error = Error.NotFound("Test.NotFound", "Not found");
        var pd = ProblemDetailsMapper.ToProblemDetails(error);

        pd.Status.Should().Be(404);
    }

    [Fact]
    public void ToProblemDetails_ConflictError_ShouldSetStatus409()
    {
        var error = Error.Conflict("Test.Conflict", "Conflict");
        var pd = ProblemDetailsMapper.ToProblemDetails(error);

        pd.Status.Should().Be(409);
    }

    [Fact]
    public void ToProblemDetails_ForbiddenError_ShouldSetStatus403()
    {
        var error = Error.Forbidden("Test.Forbidden", "Access denied");
        var pd = ProblemDetailsMapper.ToProblemDetails(error);

        pd.Status.Should().Be(403);
    }

    [Fact]
    public void ToProblemDetails_UnauthorizedError_ShouldSetStatus401()
    {
        var error = Error.Unauthorized("Test.Unauthorized", "Not authenticated");
        var pd = ProblemDetailsMapper.ToProblemDetails(error);

        pd.Status.Should().Be(401);
    }

    [Fact]
    public void ToProblemDetails_ShouldSetRfcTypeUrl()
    {
        var error = Error.NotFound("Code", "Desc");
        var pd = ProblemDetailsMapper.ToProblemDetails(error);

        pd.Type.Should().StartWith("https://www.rfc-editor.org");
    }

    [Fact]
    public void ToProblemDetails_AggregateValidationError_ShouldIncludeErrors()
    {
        var validationErrors = new[]
        {
            Error.Validation("Name", "Required"),
            Error.Validation("Email", "Invalid")
        };
        var error = new AggregateValidationError(validationErrors);
        var pd = ProblemDetailsMapper.ToProblemDetails(error);

        pd.Extensions.Should().ContainKey("errors");
    }
}

public class SingleValueObjectTests
{
    [Fact]
    public void Constructor_ShouldStoreValue()
    {
        var vo = new TestSingleValueObject(42);
        vo.Value.Should().Be(42);
    }

    [Fact]
    public void ExplicitOperator_ShouldReturnValue()
    {
        var vo = new TestSingleValueObject(42);
        int result = (int)vo;
        result.Should().Be(42);
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var vo1 = new TestSingleValueObject(42);
        var vo2 = new TestSingleValueObject(42);
        vo1.Should().Be(vo2);
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        var vo1 = new TestSingleValueObject(42);
        var vo2 = new TestSingleValueObject(99);
        vo1.Should().NotBe(vo2);
    }

    private record TestSingleValueObject(int Value) : SingleValueObject<int>(Value);
}

public class RegistrationMetricsTests
{
    [Fact]
    public void Defaults_ShouldBeZero()
    {
        var metrics = new RegistrationMetrics();
        metrics.TotalHandlersRegistered.Should().Be(0);
        metrics.TotalValidatorsRegistered.Should().Be(0);
        metrics.RegistrationDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var metrics = new RegistrationMetrics
        {
            TotalHandlersRegistered = 10,
            TotalValidatorsRegistered = 5,
            RegistrationDuration = TimeSpan.FromMilliseconds(100)
        };
        metrics.TotalHandlersRegistered.Should().Be(10);
        metrics.TotalValidatorsRegistered.Should().Be(5);
        metrics.RegistrationDuration.Should().Be(TimeSpan.FromMilliseconds(100));
    }
}

public class StartupLoggerTests
{
    [Fact]
    public void Create_ShouldReturnLogger()
    {
        var logger = StartupLogger.Create();
        logger.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithCategory_ShouldReturnLogger()
    {
        var logger = StartupLogger.Create("TestCategory");
        logger.Should().NotBeNull();
    }

    [Fact]
    public void CreateGeneric_ShouldReturnLogger()
    {
        var logger = StartupLogger.Create<StartupLoggerTests>();
        logger.Should().NotBeNull();
    }
}
