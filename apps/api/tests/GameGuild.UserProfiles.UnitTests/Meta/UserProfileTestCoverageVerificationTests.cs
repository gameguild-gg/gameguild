using System.Reflection;
using FluentAssertions;
using FluentValidation;
using GameGuild.CQRS;
using Xunit;

namespace GameGuild.Test.UserProfiles.Unit.Meta;

/// <summary>
/// Meta tests to verify comprehensive test coverage for UserProfile module components.
/// This test class ensures that all CQRS components (commands, queries, handlers, validators) 
/// and entities are accounted for in the test suite.
/// </summary>
public class UserProfileTestCoverageVerificationTests
{
    private readonly Assembly _userProfilesAssembly;
    private readonly Assembly _testAssembly;

    public UserProfileTestCoverageVerificationTests()
    {
        // Get the assembly containing UserProfile module
        _userProfilesAssembly = Assembly.GetAssembly(typeof(GameGuild.UserProfiles.UserProfile))!;

        // Get the test assembly
        _testAssembly = Assembly.GetExecutingAssembly();
    }

    [Fact]
    public void Should_Have_Test_For_All_Command_Classes()
    {
        // Arrange: Find all command classes in UserProfiles module
        var commandTypes = _userProfilesAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.UserProfiles") == true)
            .Where(t => t.Name.EndsWith("Command") && t.IsClass && !t.IsAbstract)
            .ToList();

        // Act: Find corresponding test classes
        var testTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests"))
            .ToList();

        // Assert: Every command should have a corresponding test
        var missingTests = new List<string>();

        foreach (var commandType in commandTypes)
        {
            var expectedTestName = $"{commandType.Name}Tests";
            if (!testTypes.Any(t => t.Name == expectedTestName))
            {
                missingTests.Add($"Missing test class: {expectedTestName} for command: {commandType.Name}");
            }
        }

        missingTests.Should().BeEmpty($"All commands should have corresponding test classes. Missing: {string.Join(", ", missingTests)}");
    }

    [Fact]
    public void Should_Have_Test_For_All_Query_Classes()
    {
        // Arrange: Find all query classes in UserProfiles module
        var queryTypes = _userProfilesAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.UserProfiles") == true)
            .Where(t => t.Name.EndsWith("Query") && t.IsClass && !t.IsAbstract)
            .ToList();

        // Act: Find corresponding test classes
        var testTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests"))
            .ToList();

        // Assert: Every query should have a corresponding test
        var missingTests = new List<string>();

        foreach (var queryType in queryTypes)
        {
            var expectedTestName = $"{queryType.Name}Tests";
            if (!testTypes.Any(t => t.Name == expectedTestName))
            {
                missingTests.Add($"Missing test class: {expectedTestName} for query: {queryType.Name}");
            }
        }

        missingTests.Should().BeEmpty($"All queries should have corresponding test classes. Missing: {string.Join(", ", missingTests)}");
    }

    [Fact]
    public void Should_Have_Test_For_All_Handler_Classes()
    {
        // Arrange: Find all handler classes in UserProfiles module
        var handlerTypes = _userProfilesAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.UserProfiles") == true)
            .Where(t => t.Name.EndsWith("Handler") && t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                 i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))))
            .ToList();

        // Act: Find corresponding test classes
        var testTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests"))
            .ToList();

        // Assert: Every handler should have a corresponding test
        var missingTests = new List<string>();

        foreach (var handlerType in handlerTypes)
        {
            var expectedTestName = $"{handlerType.Name}Tests";
            if (!testTypes.Any(t => t.Name == expectedTestName))
            {
                missingTests.Add($"Missing test class: {expectedTestName} for handler: {handlerType.Name}");
            }
        }

        missingTests.Should().BeEmpty($"All handlers should have corresponding test classes. Missing: {string.Join(", ", missingTests)}");
    }

    [Fact]
    public void Should_Have_Test_For_All_Validator_Classes()
    {
        // Arrange: Find all validator classes in UserProfiles module
        var validatorTypes = _userProfilesAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.UserProfiles") == true)
            .Where(t => t.Name.EndsWith("Validator") && t.IsClass && !t.IsAbstract)
            .Where(t => t.BaseType?.IsGenericType == true &&
                       t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .ToList();

        // Act: Find corresponding test classes
        var testTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests"))
            .ToList();

        // Assert: Every validator should have a corresponding test
        var missingTests = new List<string>();

        foreach (var validatorType in validatorTypes)
        {
            var expectedTestName = $"{validatorType.Name}Tests";
            if (!testTypes.Any(t => t.Name == expectedTestName))
            {
                missingTests.Add($"Missing test class: {expectedTestName} for validator: {validatorType.Name}");
            }
        }

        missingTests.Should().BeEmpty($"All validators should have corresponding test classes. Missing: {string.Join(", ", missingTests)}");
    }

    [Fact]
    public void Should_Have_Test_For_All_Entity_Classes()
    {
        // Arrange: Find all entity classes in UserProfiles module (classes that don't end with specific suffixes)
        var entityTypes = _userProfilesAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.UserProfiles") == true)
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType)
            .Where(t => !t.Name.Contains("<") && !t.Name.Contains(">")) // Exclude compiler-generated types
            .Where(t => !t.Name.EndsWith("Dto")) // Exclude DTOs
            .Where(t => !t.Name.EndsWith("Context")) // Exclude DbContext classes
            .Where(t => !t.Name.EndsWith("Statistics")) // Exclude statistics classes
            .Where(t => !t.Name.EndsWith("Command") &&
                       !t.Name.EndsWith("Query") &&
                       !t.Name.EndsWith("Handler") &&
                       !t.Name.EndsWith("Validator") &&
                       !t.Name.EndsWith("Controller") &&
                       !t.Name.EndsWith("Service") &&
                       !t.Name.EndsWith("Repository") &&
                       !t.Name.EndsWith("Response") &&
                       !t.Name.EndsWith("Request") &&
                       !t.Name.EndsWith("Extensions") &&
                       !t.Name.EndsWith("Event") &&
                       !t.Name.EndsWith("EventHandler") &&
                       !t.Name.Contains("Configuration"))
            .ToList();

        // Act: Find corresponding test classes
        var testTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests"))
            .ToList();

        // Assert: Every entity should have a corresponding test
        var missingTests = new List<string>();

        foreach (var entityType in entityTypes)
        {
            var expectedTestName = $"{entityType.Name}Tests";
            if (!testTypes.Any(t => t.Name == expectedTestName))
            {
                missingTests.Add($"Missing test class: {expectedTestName} for entity: {entityType.Name}");
            }
        }

        missingTests.Should().BeEmpty($"All entities should have corresponding test classes. Missing: {string.Join(", ", missingTests)}");
    }

    [Fact]
    public void Should_Have_Comprehensive_Coverage_Summary()
    {
        // Arrange: Get all testable types from UserProfiles module
        var allTypes = _userProfilesAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("GameGuild.UserProfiles") == true)
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var commands = allTypes.Where(t => t.Name.EndsWith("Command")).ToList();
        var queries = allTypes.Where(t => t.Name.EndsWith("Query")).ToList();
        var handlers = allTypes.Where(t => t.Name.EndsWith("Handler")).ToList();
        var validators = allTypes.Where(t => t.Name.EndsWith("Validator")).ToList();
        var entities = allTypes.Where(t => !t.Name.Contains("<") && !t.Name.Contains(">")) // Exclude compiler-generated
                               .Where(t => !t.Name.EndsWith("Dto")) // Exclude DTOs
                               .Where(t => !t.Name.EndsWith("Context")) // Exclude contexts
                               .Where(t => !t.Name.EndsWith("Statistics")) // Exclude statistics
                               .Where(t => !t.Name.EndsWith("Command") &&
                                           !t.Name.EndsWith("Query") &&
                                           !t.Name.EndsWith("Handler") &&
                                           !t.Name.EndsWith("Validator") &&
                                           !t.Name.EndsWith("Controller") &&
                                           !t.Name.EndsWith("Service") &&
                                           !t.Name.EndsWith("Repository") &&
                                           !t.Name.EndsWith("Response") &&
                                           !t.Name.EndsWith("Request") &&
                                           !t.Name.EndsWith("Extensions") &&
                                           !t.Name.EndsWith("Event") &&
                                           !t.Name.EndsWith("EventHandler") &&
                                           !t.Name.Contains("Configuration")).ToList();

        // Act: Get test classes
        var testTypes = _testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests"))
            .ToList();

        // Assert: Log coverage summary
        var totalComponents = commands.Count + queries.Count + handlers.Count + validators.Count + entities.Count;

        commands.Should().NotBeEmpty("UserProfiles module should have command classes");
        queries.Should().NotBeEmpty("UserProfiles module should have query classes");
        handlers.Should().NotBeEmpty("UserProfiles module should have handler classes");
        validators.Should().NotBeEmpty("UserProfiles module should have validator classes");
        entities.Should().NotBeEmpty("UserProfiles module should have entity classes");

        totalComponents.Should().BeGreaterThan(0, "UserProfiles module should have testable components");

        // Output coverage summary for debugging
        var summary = $"""
            UserProfiles Module Test Coverage Summary:
            - Commands: {commands.Count}
            - Queries: {queries.Count}
            - Handlers: {handlers.Count}
            - Validators: {validators.Count}
            - Entities: {entities.Count}
            - Total Components: {totalComponents}
            - Test Classes: {testTypes.Count}
            """;

        // This assertion always passes but provides useful output
        summary.Should().NotBeEmpty("Coverage summary should be available");
    }
}