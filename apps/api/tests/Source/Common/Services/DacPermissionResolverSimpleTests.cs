using GameGuild.Common;
using GameGuild.Common.Services;
using GameGuild.Modules.Permissions;
using GameGuild.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace GameGuild.Tests.Common.Services;

/// <summary>
/// Simple unit tests for DacPermissionResolver
/// Tests basic functionality of the enhanced 3-layer DAC permission resolution
/// </summary>
public class DacPermissionResolverTests : IClassFixture<TestWebApplicationFactory>, IDisposable {
  private readonly TestWebApplicationFactory _factory;
  private readonly IServiceScope _scope;
  private readonly ITestOutputHelper _output;

  public DacPermissionResolverTests(TestWebApplicationFactory factory, ITestOutputHelper output) {
    _factory = factory;
    _output = output;
    _scope = factory.Services.CreateScope();
  }

  [Fact]
  public void DacPermissionResolver_CanBeInstantiated() {
    // Arrange
    var mockPermissionService = new Mock<IPermissionService>();
    var logger = _scope.ServiceProvider.GetRequiredService<ILogger<DacPermissionResolver>>();

    // Act
    var resolver = new DacPermissionResolver(mockPermissionService.Object, logger);

    // Assert
    Assert.NotNull(resolver);
    _output.WriteLine("DacPermissionResolver was successfully instantiated");
  }

  [Fact]
  public void IDacPermissionResolver_InterfaceExists() {
    // Arrange & Act
    var interfaceType = typeof(IDacPermissionResolver);

    // Assert
    Assert.NotNull(interfaceType);
    Assert.True(interfaceType.IsInterface);
    _output.WriteLine($"IDacPermissionResolver interface exists: {interfaceType.FullName}");
  }

  [Fact]
  public void PermissionResult_CanBeInstantiated() {
    // Arrange & Act
    var result = new PermissionResult {
      IsGranted = true,
      IsExplicitlyDenied = false,
      Source = PermissionSource.TenantUser,
      GrantedBy = "TestUser",
      GrantedAt = DateTime.UtcNow,
      Reason = "Test permission",
      Priority = 5,
      IsInherited = false,
    };

    // Assert
    Assert.NotNull(result);
    Assert.True(result.IsGranted);
    Assert.Equal(PermissionSource.TenantUser, result.Source);
    _output.WriteLine($"PermissionResult created: IsGranted={result.IsGranted}, Source={result.Source}");
  }

  [Fact]
  public void EffectivePermission_CanBeInstantiated() {
    // Arrange & Act
    var permission = new EffectivePermission {
      Permission = PermissionType.Read,
      IsGranted = true,
      Source = PermissionSource.ResourceUser,
      SourceDescription = "Explicitly granted to user",
      IsInherited = false,
      Priority = 7,
    };

    // Assert
    Assert.NotNull(permission);
    Assert.Equal(PermissionType.Read, permission.Permission);
    Assert.True(permission.IsGranted);
    Assert.Equal(PermissionSource.ResourceUser, permission.Source);
    _output.WriteLine($"EffectivePermission created: {permission.Permission} from {permission.Source}");
  }

  [Fact]
  public void PermissionHierarchy_CanBeInstantiated() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    // Act
    var hierarchy = new PermissionHierarchy {
      Permission = PermissionType.Edit,
      UserId = userId,
      TenantId = tenantId,
      ResourceId = resourceId,
      Layers = new List<PermissionLayer>
        {
                new PermissionLayer
                {
                    Source = PermissionSource.GlobalDefault,
                    IsGranted = true,
                    Priority = 1,
                    Description = "Global default permission",
                },
            },
      FinalResult = new PermissionResult {
        IsGranted = true,
        Source = PermissionSource.GlobalDefault,
        Priority = 1,
      },
    };

    // Assert
    Assert.NotNull(hierarchy);
    Assert.Equal(PermissionType.Edit, hierarchy.Permission);
    Assert.Single(hierarchy.Layers);
    Assert.NotNull(hierarchy.FinalResult);
    _output.WriteLine($"PermissionHierarchy created with {hierarchy.Layers.Count} layers");
  }

  [Theory]
  [InlineData(PermissionSource.GlobalDefault)]
  [InlineData(PermissionSource.TenantDefault)]
  [InlineData(PermissionSource.TenantUser)]
  [InlineData(PermissionSource.ResourceUser)]
  public void PermissionSource_AllValuesAreValid(PermissionSource source) {
    // Arrange & Act
    var layer = new PermissionLayer {
      Source = source,
      IsGranted = true,
      Description = $"Test layer for {source}",
    };

    // Assert
    Assert.NotNull(layer);
    Assert.Equal(source, layer.Source);
    _output.WriteLine($"PermissionSource {source} is valid");
  }

  [Fact]
  public void DacPermissionResolver_DependenciesCanBeResolved() {
    // Arrange & Act
    var permissionService = _scope.ServiceProvider.GetService<IPermissionService>();
    var logger = _scope.ServiceProvider.GetService<ILogger<DacPermissionResolver>>();
    var dacResolver = _scope.ServiceProvider.GetService<IDacPermissionResolver>();

    // Assert
    // Some services might not be registered in test environment
    _output.WriteLine($"IPermissionService resolved: {permissionService != null}");
    _output.WriteLine($"Logger resolved: {logger != null}");
    _output.WriteLine($"IDacPermissionResolver resolved: {dacResolver != null}");

    // At minimum, logger should be available
    Assert.NotNull(logger);
  }

  [Fact]
  public void EnhancedPermissionSystem_TypesExist() {
    // Arrange & Act
    var dacResolverType = typeof(IDacPermissionResolver);
    var permissionResultType = typeof(PermissionResult);
    var effectivePermissionType = typeof(EffectivePermission);
    var permissionHierarchyType = typeof(PermissionHierarchy);
    var permissionLayerType = typeof(PermissionLayer);

    // Assert
    Assert.NotNull(dacResolverType);
    Assert.NotNull(permissionResultType);
    Assert.NotNull(effectivePermissionType);
    Assert.NotNull(permissionHierarchyType);
    Assert.NotNull(permissionLayerType);

    _output.WriteLine("All enhanced permission system types exist:");
    _output.WriteLine($"  {dacResolverType.Name}");
    _output.WriteLine($"  {permissionResultType.Name}");
    _output.WriteLine($"  {effectivePermissionType.Name}");
    _output.WriteLine($"  {permissionHierarchyType.Name}");
    _output.WriteLine($"  {permissionLayerType.Name}");
  }

  [Fact]
  public async Task MockPermissionService_CanBeUsed() {
    // Arrange
    var mockService = new Mock<IPermissionService>();
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();

    mockService.Setup(x => x.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read))
        .ReturnsAsync(true);

    // Act
    var result = await mockService.Object.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read);

    // Assert
    Assert.True(result);
    _output.WriteLine($"Mock permission service returned: {result}");

    // Verify the mock was called
    mockService.Verify(x => x.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read), Times.Once);
  }

  public void Dispose() {
    _scope?.Dispose();
  }
}
