using System.Reflection;
using FluentAssertions;
using GameGuild.Permissions;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Meta;

/// <summary>
/// Meta tests to verify comprehensive test coverage for the Permissions module
/// Ensures all CQRS components have corresponding test classes
/// </summary>
public class PermissionTestCoverageVerificationTests
{
    private readonly Assembly _permissionsAssembly;
    private readonly Assembly _testAssembly;

    public PermissionTestCoverageVerificationTests()
    {
        _permissionsAssembly = typeof(TenantPermission).Assembly;
        _testAssembly = typeof(PermissionTestCoverageVerificationTests).Assembly;
    }

    [Fact]
    public void AllCommands_ShouldHaveCorrespondingHandlerTests()
    {
        // Arrange
        var commandTypes = _permissionsAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Permissions") == true &&
                       t.Name.EndsWith("Command") && !t.IsAbstract)
            .ToList();

        // Act & Assert
        foreach (var commandType in commandTypes)
        {
            // Remove "Command" suffix from the command name to match handler naming convention
            var handlerName = commandType.Name.Replace("Command", "");
            var expectedHandlerTestName = $"{handlerName}HandlerTests";
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
        var queryTypes = _permissionsAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Permissions") == true &&
                       t.Name.EndsWith("Query") && !t.IsAbstract)
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
        var validatorTypes = _permissionsAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Permissions") == true &&
                       t.Name.EndsWith("Validator") && !t.IsAbstract)
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
        // Arrange
        var entityTypes = _permissionsAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Permissions") == true &&
                       (t.Name.EndsWith("Permission") || t.Name.EndsWith("Template") ||
                        t.Name.EndsWith("Audit") || t.Name.EndsWith("Delegation")) &&
                       !t.IsAbstract && !t.Name.Contains("Configuration"))
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
    public void AllHandlerTestClasses_ShouldFollowNamingConvention()
    {
        // Arrange
        var handlerTestTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("HandlerTests"))
            .ToList();

        // Act & Assert
        foreach (var testType in handlerTestTypes)
        {
            testType.Namespace.Should().StartWith("GameGuild.Tests.Permissions.Unit.Handlers",
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
            testType.Namespace.Should().StartWith("GameGuild.Tests.Permissions.Unit.Validators",
                $"Validator test {testType.Name} should be in the correct namespace");
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
                       !t.Name.Contains("Coverage") &&
                       !t.Name.Contains("Meta"))
            .ToList();

        // Act & Assert
        foreach (var testType in entityTestTypes)
        {
            testType.Namespace.Should().StartWith("GameGuild.Tests.Permissions.Unit",
                $"Entity test {testType.Name} should be in the correct namespace");
        }
    }

    [Fact]
    public void AllTestClasses_ShouldHaveProperTestMethods()
    {
        // Arrange
        var testTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests") &&
                        t.Namespace?.StartsWith("GameGuild.Tests.Permissions.Unit") == true &&
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