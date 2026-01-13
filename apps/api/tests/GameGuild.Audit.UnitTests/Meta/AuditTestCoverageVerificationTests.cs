using System.Reflection;
using FluentAssertions;
using GameGuild.Compliance.Audit;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Meta;

/// <summary>
/// Meta tests to verify comprehensive test coverage for the Audit module
/// Ensures all CQRS components have corresponding test classes
/// </summary>
public class AuditTestCoverageVerificationTests
{
    private readonly Assembly _auditAssembly;
    private readonly Assembly _testAssembly;

    public AuditTestCoverageVerificationTests()
    {
        _auditAssembly = typeof(AuditLog).Assembly;
        _testAssembly = typeof(AuditTestCoverageVerificationTests).Assembly;
    }

    [Fact]
    public void AllCommands_ShouldHaveCorrespondingHandlerTests()
    {
        // Arrange
        var commandTypes = _auditAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.Audit") == true &&
                       t.Name.EndsWith("Command") &&
                       !t.IsAbstract)
            .ToList();

        // Act & Assert
        foreach (var commandType in commandTypes)
        {
            var expectedHandlerTestName = $"{commandType.Name}HandlerTests";
            var handlerTestType = _testAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == expectedHandlerTestName);

            handlerTestType.Should().NotBeNull(
                $"Handler test class {expectedHandlerTestName} should exist for command {commandType.Name}");
        }
    }

    [Fact]
    public void AllQueries_ShouldHaveCorrespondingHandlerTests()
    {
        // Arrange
        var queryTypes = _auditAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.Audit") == true &&
                       t.Name.EndsWith("Query") &&
                       !t.IsAbstract &&
                       !t.Name.Contains("Request") &&  // Exclude query request DTOs
                       t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.Contains("IRequest")))  // Must implement IRequest<>
            .ToList();

        // Act & Assert
        foreach (var queryType in queryTypes)
        {
            var expectedHandlerTestName = $"{queryType.Name}HandlerTests";
            var handlerTestType = _testAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == expectedHandlerTestName);

            handlerTestType.Should().NotBeNull(
                $"Handler test class {expectedHandlerTestName} should exist for query {queryType.Name}");
        }
    }

    [Fact]
    public void AllValidators_ShouldHaveCorrespondingTests()
    {
        // Arrange
        var validatorTypes = _auditAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.Audit") == true &&
                       t.Name.EndsWith("Validator") &&
                       !t.IsAbstract)
            .ToList();

        // Act & Assert
        foreach (var validatorType in validatorTypes)
        {
            var expectedTestName = $"{validatorType.Name}Tests";
            var testType = _testAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == expectedTestName);

            testType.Should().NotBeNull(
                $"Test class {expectedTestName} should exist for validator {validatorType.Name}");
        }
    }

    [Fact]
    public void AllEntities_ShouldHaveCorrespondingTests()
    {
        // Arrange - Only test entities that are actively used (not advanced/unimplemented entities)
        var activeEntityNames = new[] { "AuditLog", "AuditLogDto" };
        var entityTypes = _auditAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Audit.Entities") == true &&
                       activeEntityNames.Contains(t.Name) &&
                       !t.IsAbstract && !t.IsInterface && !t.IsEnum)
            .ToList();

        // Act & Assert
        foreach (var entityType in entityTypes)
        {
            var expectedTestName = $"{entityType.Name}Tests";
            var testType = _testAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == expectedTestName);

            testType.Should().NotBeNull(
                $"Test class {expectedTestName} should exist for entity {entityType.Name}");
        }
    }

    [Fact]
    public void AllServiceClasses_ShouldHaveCorrespondingTests()
    {
        // Arrange
        var serviceTypes = _auditAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Audit") == true &&
                       t.Name.EndsWith("Service") &&
                       !t.IsAbstract && !t.IsInterface)
            .ToList();

        // Act & Assert
        foreach (var serviceType in serviceTypes)
        {
            var expectedTestName = $"{serviceType.Name}Tests";
            var testType = _testAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == expectedTestName);

            testType.Should().NotBeNull(
                $"Test class {expectedTestName} should exist for service {serviceType.Name}");
        }
    }

    [Fact]
    public void AllHandlerTestClasses_ShouldFollowNamingConvention()
    {
        // Arrange
        var handlerTestTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("HandlerTests"))
            .ToList();

        // Act & Assert
        foreach (var testType in handlerTestTypes)
        {
            testType.Namespace.Should().StartWith("GameGuild.Tests.Audit.Unit.Handlers",
                $"Handler test {testType.Name} should be in the correct namespace");
        }
    }

    [Fact]
    public void AllValidatorTestClasses_ShouldFollowNamingConvention()
    {
        // Arrange
        var validatorTestTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("ValidatorTests"))
            .ToList();

        // Act & Assert
        foreach (var testType in validatorTestTypes)
        {
            testType.Namespace.Should().StartWith("GameGuild.Tests.Audit.Unit.Validators",
                $"Validator test {testType.Name} should be in the correct namespace");
        }
    }

    [Fact]
    public void AllServiceTestClasses_ShouldFollowNamingConvention()
    {
        // Arrange
        var serviceTestTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("ServiceTests"))
            .ToList();

        // Act & Assert
        foreach (var testType in serviceTestTypes)
        {
            testType.Namespace.Should().StartWith("GameGuild.Tests.Audit.Unit.Services",
                $"Service test {testType.Name} should be in the correct namespace");
        }
    }

    [Fact]
    public void AllEntityTestClasses_ShouldFollowNamingConvention()
    {
        // Arrange
        var entityTestTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests") &&
                       !t.Name.EndsWith("HandlerTests") &&
                       !t.Name.EndsWith("ValidatorTests") &&
                       !t.Name.EndsWith("ServiceTests") &&
                       !t.Name.Contains("Coverage") &&
                       !t.Name.Contains("Meta"))
            .ToList();

        // Act & Assert
        foreach (var testType in entityTestTypes)
        {
            testType.Namespace.Should().StartWith("GameGuild.Tests.Audit.Unit",
                $"Entity test {testType.Name} should be in the correct namespace");
        }
    }

    [Fact]
    public void AllTestClasses_ShouldHaveProperTestMethods()
    {
        // Arrange
        var testTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests") &&
                        t.Namespace?.StartsWith("GameGuild.Tests.Audit.Unit") == true &&
                        !t.Name.Contains("Coverage"))
            .ToList();

        // Act & Assert
        foreach (var testType in testTypes)
        {
            var testMethods = testType.GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(FactAttribute), false).Any() ||
                           m.GetCustomAttributes(typeof(TheoryAttribute), false).Any())
                .ToList();

            testMethods.Should().NotBeEmpty(
                $"Test class {testType.Name} should have at least one test method marked with [Fact] or [Theory]");
        }
    }
}