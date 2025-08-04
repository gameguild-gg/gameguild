using System.Reflection;
using GameGuild.Tests.MockModules;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Debug;

/// <summary>
/// Test to verify type loading for TestModuleQueries
/// </summary>
public class TypeLoadingTest
{
    private readonly ITestOutputHelper _output;

    public TypeLoadingTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestModuleQueries_Should_Be_Loadable()
    {
        // First, check if we can access the type directly
        var directType = typeof(TestModuleQueries);
        _output.WriteLine($"Direct type access: {directType.FullName}");
        _output.WriteLine($"Assembly: {directType.Assembly.FullName}");
        
        // Check current assembly
        var currentAssembly = Assembly.GetExecutingAssembly();
        _output.WriteLine($"Current assembly: {currentAssembly.FullName}");
        
        // Try the reflection approach used by the main application
        var reflectionType = Type.GetType("GameGuild.Tests.MockModules.TestModuleQueries, GameGuild.API.Tests");
        _output.WriteLine($"Type.GetType with assembly qualification: {reflectionType != null}");
        
        if (reflectionType == null)
        {
            // Try without assembly name
            var simpleType = Type.GetType("GameGuild.Tests.MockModules.TestModuleQueries");
            _output.WriteLine($"Type.GetType without assembly: {simpleType != null}");
            
            // Try with current assembly
            var currentAssemblyType = currentAssembly.GetType("GameGuild.Tests.MockModules.TestModuleQueries");
            _output.WriteLine($"GetType from current assembly: {currentAssemblyType != null}");
        }
        
        Assert.True(directType != null, "Should be able to access TestModuleQueries directly");
    }
}
