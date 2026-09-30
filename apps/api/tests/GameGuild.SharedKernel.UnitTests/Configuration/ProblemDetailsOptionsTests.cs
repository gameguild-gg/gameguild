using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.ProblemDetails;
using Microsoft.Extensions.Configuration;

namespace GameGuild.SharedKernel.UnitTests.Configuration;

public sealed class ProblemDetailsOptionsTests
{
    [Fact]
    public void Defaults_ShouldValidateAndKeepExceptionDetailsDisabled()
    {
        var options = ProblemDetailsOptions.CreateDefault();

        options.Validate();

        options.IncludeExceptionDetails.Should().BeFalse();
        options.DetailLevel.Should().Be(ProblemDetailsDetailLevel.Standard);
        options.IncludeTraceId.Should().BeTrue();
        options.IncludeCorrelationId.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldRequireDetailedLevelBeforeIncludingExceptionText()
    {
        var options = ProblemDetailsOptions.CreateDefault();
        options.IncludeExceptionDetails = true;

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("IncludeExceptionDetails requires DetailLevel to be Detailed.");
    }

    [Fact]
    public void Validate_ShouldRejectInvalidExceptionStatusCode()
    {
        var options = ProblemDetailsOptions.CreateDefault();
        options.ExceptionMappings[typeof(ArgumentException).FullName!] = new()
        {
            StatusCode = 200,
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("ExceptionMappings[System.ArgumentException].StatusCode must be an HTTP error status code between 400 and 599.");
    }

    [Fact]
    public void Validate_ShouldRejectMalformedCorrelationHeaderName()
    {
        var options = ProblemDetailsOptions.CreateDefault();
        options.CorrelationIdHeaderName = "X-Correlation-ID\r\nInjected: true";

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("CorrelationIdHeaderName must be a valid HTTP header name.");
    }

    [Fact]
    public void Validate_ShouldRejectReservedExtensionNames()
    {
        var options = ProblemDetailsOptions.CreateDefault();
        options.CustomExtensions["status"] = "custom";

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Custom extension name 'status' is reserved.");
    }

    [Fact]
    public void Validate_ShouldRejectUnknownLocaleNames()
    {
        var options = ProblemDetailsOptions.CreateDefault();
        options.LocalizedMessages["en_US"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = new() { Title = "Erro", Detail = "Algo falhou." },
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("LocalizedMessages contains an invalid culture name 'en_US'.");
    }

    [Fact]
    public void Builder_ShouldBindAndValidateNestedOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProblemDetails:DetailLevel"] = "Minimal",
                ["ProblemDetails:IncludeTraceId"] = "false",
                ["ProblemDetails:ExceptionMappings:System.ArgumentException:StatusCode"] = "400",
                ["ProblemDetails:ExceptionMappings:System.ArgumentException:Type"] = "https://api.gameguild.gg/problems/invalid-request",
                ["ProblemDetails:ExceptionMappings:System.ArgumentException:Title"] = "Invalid request",
                ["ProblemDetails:ExceptionMappings:System.ArgumentException:LocalizedMessageKey"] = "invalid-request",
                ["ProblemDetails:LocalizedMessages:pt-BR:invalid-request:Title"] = "Solicitação inválida",
            })
            .Build();

        var options = ProblemDetailsOptionsBuilder.Build(configuration);

        options.DetailLevel.Should().Be(ProblemDetailsDetailLevel.Minimal);
        options.IncludeTraceId.Should().BeFalse();
        options.ExceptionMappings[typeof(ArgumentException).FullName!].LocalizedMessageKey.Should().Be("invalid-request");
        options.LocalizedMessages["pt-BR"]["invalid-request"].Title.Should().Be("Solicitação inválida");
    }

    [Fact]
    public void Validate_ShouldRejectUndefinedDetailLevel()
    {
        var options = ProblemDetailsOptions.CreateDefault();
        options.DetailLevel = (ProblemDetailsDetailLevel)99;

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Problem Details option 'DetailLevel' is not supported.");
    }
}
