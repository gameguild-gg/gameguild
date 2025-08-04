using System.Reflection;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Debug;

/// <summary>
/// Debug test to check assembly names and type loading
/// </summary>
public class AssemblyDebugTest
{
    private readonly ITestOutputHelper _output;

    public AssemblyDebugTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_Assembly_And_Type_Information()
    {
        // Check current assembly name
        var currentAssembly = Assembly.GetExecutingAssembly();
        _output.WriteLine($"Current Assembly: {currentAssembly.FullName}");
        
        // Check all loaded assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (assembly.FullName?.Contains("GameGuild") == true)
            {
                _output.WriteLine($"GameGuild Assembly: {assembly.FullName}");
            }
        }

        // Try to load the TestModuleQueries type
        var testModuleType = Type.GetType("GameGuild.Tests.MockModules.TestModuleQueries, GameGuild.API.Tests");
        _output.WriteLine($"TestModuleQueries type found: {testModuleType != null}");
        
        if (testModuleType == null)
        {
            // Try different assembly qualifications
            var alternativeType = Type.GetType("GameGuild.Tests.MockModules.TestModuleQueries");
            _output.WriteLine($"TestModuleQueries without assembly qualification: {alternativeType != null}");
            
            // List all types in the current assembly
            var types = currentAssembly.GetTypes()
                .Where(t => t.Name.Contains("TestModule"))
                .ToList();
            
            _output.WriteLine($"TestModule types in current assembly: {types.Count}");
            foreach (var type in types)
            {
                _output.WriteLine($"  - {type.FullName}");
            }
        }
        
        Assert.True(true, "Debug complete");
    }
}
