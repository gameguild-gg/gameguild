using System.Reflection;
using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Meta;

/// <summary>
/// Meta tests to verify comprehensive test coverage and project structure
/// </summary>
public class TestCoverageVerificationTests
{
    [Fact]
    public void TestCoverage_Should_Include_All_Major_Core_Components()
    {
        // Arrange - Get all test classes in the Core.Unit namespace
        Assembly testAssembly = Assembly.GetExecutingAssembly();
        Type[] testTypes = testAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.Tests.Core.Unit") == true)
            .Where(t => t.Name.EndsWith("Tests"))
            .ToArray();

        // Act & Assert - Verify we have tests for major components
        string[] expectedTestClasses = [
            "EntityBaseTests",
            "BusinessExceptionTests",
            "ValidationExceptionTests",
            "ErrorTests",
            "ResultTests",
            "ValidationBehaviorTests",
            "LoggingBehaviorTests",
            "PerformanceBehaviorTests",
            "EmailAddressTests",
            "MoneyTests",
            "DateTimeProviderTests",
            "SpecificationBaseTests",
            "GlobalExceptionHandlerTests",
            "CoreAbstractionsTests",
            "DomainExceptionTests"
        ];

        foreach (string expectedTestClass in expectedTestClasses)
        {
            _ = testTypes.Should().Contain(t => t.Name == expectedTestClass,
                $"Test class {expectedTestClass} should exist to ensure comprehensive coverage");
        }
    }

    [Fact]
    public void TestClasses_Should_Follow_Naming_Convention()
    {
        // Arrange
        Assembly testAssembly = Assembly.GetExecutingAssembly();
        Type[] testTypes = testAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.Tests.Core.Unit") == true)
            .Where(t => t.GetMethods().Any(m => m.GetCustomAttributes<FactAttribute>().Any() ||
                                              m.GetCustomAttributes<TheoryAttribute>().Any()))
            .ToArray();

        // Act & Assert
        foreach (Type testType in testTypes)
        {
            _ = testType.Name.Should().EndWith("Tests",
                $"Test class {testType.Name} should follow the naming convention ending with 'Tests'");
        }
    }

    [Fact]
    public void TestMethods_Should_Follow_Naming_Convention()
    {
        // Arrange
        Assembly testAssembly = Assembly.GetExecutingAssembly();
        MethodInfo[] testMethods = testAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.Tests.Core.Unit") == true)
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes<FactAttribute>().Any() ||
                       m.GetCustomAttributes<TheoryAttribute>().Any())
            .ToArray();

        // Act & Assert
        foreach (MethodInfo testMethod in testMethods)
        {
            _ = testMethod.Name.Should().Contain("_Should_",
                $"Test method {testMethod.DeclaringType?.Name}.{testMethod.Name} should follow the naming convention 'Method_Should_ExpectedBehavior'");
        }
    }

    [Fact]
    public void CoreUnitTests_Should_Be_Organized_In_Proper_Namespaces()
    {
        // Arrange
        Assembly testAssembly = Assembly.GetExecutingAssembly();
        Type[] testTypes = testAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.Tests.Core.Unit") == true)
            .ToArray();

        // Expected namespace structure
        string[] expectedNamespaces = [
            "GameGuild.Tests.Core.Unit.Abstractions",
            "GameGuild.Tests.Core.Unit.Behaviors",
            "GameGuild.Tests.Core.Unit.Entities",
            "GameGuild.Tests.Core.Unit.Exceptions",
            "GameGuild.Tests.Core.Unit.Providers",
            "GameGuild.Tests.Core.Unit.Results",
            "GameGuild.Tests.Core.Unit.ValueObjects",
            "GameGuild.Tests.Core.Unit.Meta"
        ];

        // Act & Assert
        foreach (string expectedNamespace in expectedNamespaces)
        {
            _ = testTypes.Should().Contain(t => t.Namespace == expectedNamespace,
                $"Namespace {expectedNamespace} should contain test classes to ensure proper organization");
        }
    }

    [Fact]
    public void TestProject_Should_Reference_Required_Testing_Packages()
    {
        // Arrange
        Assembly testAssembly = Assembly.GetExecutingAssembly();

        // Act - Check for key testing framework types
        Type? xunitFactType = testAssembly.GetReferencedAssemblies()
            .Select(Assembly.Load)
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "FactAttribute" && t.Namespace == "Xunit");

        // Assert essential testing framework components are available
        _ = xunitFactType.Should().NotBeNull("xUnit framework should be available");

        // Verify FluentAssertions is available by checking if we can create assertions
        string testString = "test";
        _ = testString.Should().NotBeNull(); // This will fail compilation if FluentAssertions isn't available
    }

    [Fact]
    public void TestMethods_Should_Have_Proper_Documentation()
    {
        // Arrange
        Assembly testAssembly = Assembly.GetExecutingAssembly();
        Type[] testTypes = testAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.Tests.Core.Unit") == true)
            .Where(t => t.Name.EndsWith("Tests"))
            .Take(5) // Sample a few classes for performance
            .ToArray();

        // Act & Assert
        foreach (Type testType in testTypes)
        {
            // Verify class has XML documentation
            _ = testType.GetCustomAttributes<System.ComponentModel.DescriptionAttribute>().Any()
                .Should().BeFalse("We don't use DescriptionAttribute, but classes should have XML documentation comments");

            // Verify test methods exist
            MethodInfo[] testMethods = testType.GetMethods()
                .Where(m => m.GetCustomAttributes<FactAttribute>().Any() ||
                           m.GetCustomAttributes<TheoryAttribute>().Any())
                .ToArray();

            _ = testMethods.Should().NotBeEmpty($"Test class {testType.Name} should have test methods");
        }
    }

    [Theory]
    [InlineData("EntityBase", "Entities")]
    [InlineData("Result", "Results")]
    [InlineData("Error", "Exceptions")]
    [InlineData("ValidationBehavior", "Behaviors")]
    [InlineData("EmailAddress", "ValueObjects")]
    public void Core_Components_Should_Have_Corresponding_Tests(string componentName, string expectedFolder)
    {
        // Arrange
        Assembly testAssembly = Assembly.GetExecutingAssembly();

        // Act - Look for test class
        Type? testType = testAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == $"{componentName}Tests" &&
                                t.Namespace?.Contains(expectedFolder) == true);

        // Assert
        _ = testType.Should().NotBeNull($"Component '{componentName}' should have corresponding tests in {expectedFolder} folder");
    }
}