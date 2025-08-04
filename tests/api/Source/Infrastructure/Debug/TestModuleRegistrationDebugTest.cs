using System.Reflection;
using GameGuild.Tests.MockModules;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Debug;

/// <summary>
/// Debug test to verify TestModule registration behavior
/// </summary>
public class TestModuleRegistrationDebugTest
{
    private readonly ITestOutputHelper _output;

    public TestModuleRegistrationDebugTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Should_Be_Able_To_Load_TestModuleQueries_Type()
    {
        // Test type loading using the same approach as the infrastructure
        var testModuleType = Type.GetType("GameGuild.Tests.MockModules.TestModuleQueries, GameGuild.API.Tests");
        
        _output.WriteLine($"TestModuleQueries type found: {testModuleType != null}");
        _output.WriteLine($"Current assembly: {Assembly.GetExecutingAssembly().FullName}");
        
        if (testModuleType != null)
        {
            _output.WriteLine($"Type full name: {testModuleType.FullName}");
            _output.WriteLine($"Type assembly: {testModuleType.Assembly.FullName}");
        }
        
        // Also test direct type reference
        var directType = typeof(TestModuleQueries);
        _output.WriteLine($"Direct type reference works: {directType != null}");
        _output.WriteLine($"Direct type full name: {directType.FullName}");
        
        Assert.NotNull(testModuleType);
        Assert.Equal(directType, testModuleType);
    }

    [Fact]
    public void Should_Detect_Test_Environment_Assembly()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var testAssembly = assemblies.FirstOrDefault(a => a.FullName?.Contains("GameGuild.API.Tests") == true);
        
        _output.WriteLine($"Total assemblies loaded: {assemblies.Length}");
        _output.WriteLine($"GameGuild.API.Tests assembly found: {testAssembly != null}");
        
        if (testAssembly != null)
        {
            _output.WriteLine($"Test assembly full name: {testAssembly.FullName}");
        }
        
        foreach (var assembly in assemblies.Where(a => a.FullName?.Contains("GameGuild") == true))
        {
            _output.WriteLine($"GameGuild assembly: {assembly.FullName}");
        }
        
        Assert.NotNull(testAssembly);
    }

    [Fact]
    public void Should_Verify_GraphQL_Options_For_Testing()
    {
        var options = GameGuild.Common.DependencyInjection.GraphQLOptionsFactory.ForTesting();
        
        _output.WriteLine($"IsTestEnvironment: {options.IsTestEnvironment}");
        _output.WriteLine($"EnableTestingModule: {options.EnableTestingModule}");
        _output.WriteLine($"EnableAuthentication: {options.EnableAuthentication}");
        _output.WriteLine($"EnableAdvancedModules: {options.EnableAdvancedModules}");
        _output.WriteLine($"EnableAuthorization: {options.EnableAuthorization}");
        
        Assert.True(options.IsTestEnvironment);
        Assert.True(options.EnableTestingModule);
    }
}
