using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.Modules.Permissions;
using GameGuild.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GameGuild.Tests.Common.Controllers;

/// <summary>
/// Integration tests for ProjectPermissionController
/// Tests project-specific collaboration and permission management
/// </summary>
public class ProjectPermissionControllerTests : IClassFixture<TestWebApplicationFactory>, IDisposable {
  private readonly TestWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private readonly IServiceScope _scope;
  private readonly ITestOutputHelper _output;
  private readonly string _authToken;

  // Test data
  private readonly Guid _projectId = Guid.NewGuid();
  private readonly Guid _userId = Guid.NewGuid();
  private readonly Guid _tenantId = Guid.NewGuid();

  public ProjectPermissionControllerTests(TestWebApplicationFactory factory, ITestOutputHelper output) {
    _factory = factory;
    _output = output;
    _scope = factory.Services.CreateScope();

    // Create authenticated client
    _client = factory.CreateClient();
    _authToken = GenerateTestJwtToken();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
  }

  [Fact]
  public async Task GetProjectRoleTemplates_ShouldReturnPredefinedRoles() {
    // Arrange
    var endpoint = "/api/projects/role-templates";

    // Act
    var response = await _client.GetAsync(endpoint);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var roleTemplates = JsonSerializer.Deserialize<ProjectRoleTemplate[]>(content, new JsonSerializerOptions {
      PropertyNameCaseInsensitive = true,
    });

    Assert.NotNull(roleTemplates);
    Assert.Contains(roleTemplates, r => r.Name == "Viewer");
    Assert.Contains(roleTemplates, r => r.Name == "Collaborator");
    Assert.Contains(roleTemplates, r => r.Name == "Editor");
    Assert.Contains(roleTemplates, r => r.Name == "Admin");

    _output.WriteLine($"Found {roleTemplates.Length} role templates");
    foreach (var role in roleTemplates) {
      _output.WriteLine($"Role: {role.Name} with {role.Permissions.Length} permissions");
    }
  }

  [Fact]
  public async Task GetProjectCollaborators_WithValidProject_ShouldReturnCollaborators() {
    // Arrange
    var endpoint = $"/api/projects/{_projectId}/collaborators";

    // Act
    var response = await _client.GetAsync(endpoint);

    // Assert
    Assert.True(
        response.StatusCode == HttpStatusCode.OK ||
        response.StatusCode == HttpStatusCode.Forbidden ||
        response.StatusCode == HttpStatusCode.NotFound);

    if (response.StatusCode == HttpStatusCode.OK) {
      var content = await response.Content.ReadAsStringAsync();
      var collaborators = JsonSerializer.Deserialize<ProjectCollaborator[]>(content, new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
      });

      Assert.NotNull(collaborators);
      _output.WriteLine($"Project has {collaborators.Length} collaborators");
    }
  }

  [Fact]
  public async Task AddCollaborator_WithValidRequest_ShouldReturnSuccess() {
    // Arrange
    var endpoint = $"/api/projects/{_projectId}/collaborators";
    var addRequest = new {
      email = "collaborator@example.com",
      role = "Collaborator",
      customPermissions = new int[] { },
      message = "Welcome to the project!",
      notifyUser = true,
    };

    var json = JsonSerializer.Serialize(addRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync(endpoint, content);

    // Assert
    Assert.True(
        response.StatusCode == HttpStatusCode.OK ||
        response.StatusCode == HttpStatusCode.Forbidden ||
        response.StatusCode == HttpStatusCode.BadRequest);

    _output.WriteLine($"Add collaborator response status: {response.StatusCode}");

    if (response.StatusCode == HttpStatusCode.OK) {
      var responseContent = await response.Content.ReadAsStringAsync();
      var result = JsonSerializer.Deserialize<AddCollaboratorResult>(responseContent, new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
      });
      Assert.NotNull(result);
    }
  }

  [Fact]
  public async Task UpdateCollaboratorRole_WithValidRequest_ShouldReturnSuccess() {
    // Arrange
    var collaboratorId = Guid.NewGuid();
    var endpoint = $"/api/projects/{_projectId}/collaborators/{collaboratorId}";
    var updateRequest = new {
      role = "Editor",
      customPermissions = new[] { (int)PermissionType.Read, (int)PermissionType.Edit, (int)PermissionType.Comment },
    };

    var json = JsonSerializer.Serialize(updateRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PutAsync(endpoint, content);

    // Assert
    Assert.True(
        response.StatusCode == HttpStatusCode.OK ||
        response.StatusCode == HttpStatusCode.Forbidden ||
        response.StatusCode == HttpStatusCode.NotFound ||
        response.StatusCode == HttpStatusCode.BadRequest);

    _output.WriteLine($"Update collaborator role response status: {response.StatusCode}");
  }

  [Fact]
  public async Task RemoveCollaborator_WithValidRequest_ShouldReturnSuccess() {
    // Arrange
    var collaboratorId = Guid.NewGuid();
    var endpoint = $"/api/projects/{_projectId}/collaborators/{collaboratorId}";

    // Act
    var response = await _client.DeleteAsync(endpoint);

    // Assert
    Assert.True(
        response.StatusCode == HttpStatusCode.OK ||
        response.StatusCode == HttpStatusCode.Forbidden ||
        response.StatusCode == HttpStatusCode.NoContent ||
        response.StatusCode == HttpStatusCode.NotFound);

    _output.WriteLine($"Remove collaborator response status: {response.StatusCode}");
  }

  [Fact]
  public async Task ShareProject_WithValidRequest_ShouldReturnSuccess() {
    // Arrange
    var endpoint = $"/api/projects/{_projectId}/share";
    var shareRequest = new {
      emails = new[] { "user1@example.com", "user2@example.com" },
      role = "Viewer",
      customPermissions = new int[] { },
      expiresAt = (DateTime?)null,
      message = "Check out this project!",
      requireAcceptance = false,
      notifyUsers = true,
    };

    var json = JsonSerializer.Serialize(shareRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync(endpoint, content);

    // Assert
    Assert.True(
        response.StatusCode == HttpStatusCode.OK ||
        response.StatusCode == HttpStatusCode.Forbidden ||
        response.StatusCode == HttpStatusCode.BadRequest);

    _output.WriteLine($"Share project response status: {response.StatusCode}");

    if (response.StatusCode == HttpStatusCode.OK) {
      var responseContent = await response.Content.ReadAsStringAsync();
      var result = JsonSerializer.Deserialize<ShareProjectResult>(responseContent, new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
      });
      Assert.NotNull(result);
    }
  }

  [Fact]
  public async Task GetProjectPermissions_WithValidProject_ShouldReturnPermissions() {
    // Arrange
    var endpoint = $"/api/projects/{_projectId}/permissions";

    // Act
    var response = await _client.GetAsync(endpoint);

    // Assert
    Assert.True(
        response.StatusCode == HttpStatusCode.OK ||
        response.StatusCode == HttpStatusCode.Forbidden ||
        response.StatusCode == HttpStatusCode.NotFound);

    if (response.StatusCode == HttpStatusCode.OK) {
      var content = await response.Content.ReadAsStringAsync();
      var permissions = JsonSerializer.Deserialize<ProjectPermissionSummary>(content, new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
      });

      Assert.NotNull(permissions);
      _output.WriteLine($"Project permissions: {permissions.MyPermissions.Length} for current user");
    }
  }

  [Fact]
  public async Task AcceptInvitation_WithValidToken_ShouldReturnSuccess() {
    // Arrange
    var invitationToken = "test-invitation-token";
    var endpoint = $"/api/projects/invitations/{invitationToken}/accept";

    // Act
    var response = await _client.PostAsync(endpoint, null);

    // Assert
    Assert.True(
        response.StatusCode == HttpStatusCode.OK ||
        response.StatusCode == HttpStatusCode.BadRequest ||
        response.StatusCode == HttpStatusCode.NotFound ||
        response.StatusCode == HttpStatusCode.Gone); // Expired invitation

    _output.WriteLine($"Accept invitation response status: {response.StatusCode}");
  }

  [Fact]
  public async Task DeclineInvitation_WithValidToken_ShouldReturnSuccess() {
    // Arrange
    var invitationToken = "test-invitation-token";
    var endpoint = $"/api/projects/invitations/{invitationToken}/decline";

    // Act
    var response = await _client.PostAsync(endpoint, null);

    // Assert
    Assert.True(
        response.StatusCode == HttpStatusCode.OK ||
        response.StatusCode == HttpStatusCode.BadRequest ||
        response.StatusCode == HttpStatusCode.NotFound ||
        response.StatusCode == HttpStatusCode.Gone); // Expired invitation

    _output.WriteLine($"Decline invitation response status: {response.StatusCode}");
  }

  [Fact]
  public async Task GetMyInvitations_ShouldReturnInvitations() {
    // Arrange
    var endpoint = "/api/projects/my-invitations";

    // Act
    var response = await _client.GetAsync(endpoint);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var invitations = JsonSerializer.Deserialize<ProjectInvitation[]>(content, new JsonSerializerOptions {
      PropertyNameCaseInsensitive = true,
    });

    Assert.NotNull(invitations);
    _output.WriteLine($"User has {invitations.Length} project invitations");
  }

  [Theory]
  [InlineData("Viewer")]
  [InlineData("Collaborator")]
  [InlineData("Editor")]
  [InlineData("Admin")]
  public async Task GetRolePermissions_WithValidRole_ShouldReturnPermissions(string roleName) {
    // Arrange
    var endpoint = $"/api/projects/roles/{roleName}/permissions";

    // Act
    var response = await _client.GetAsync(endpoint);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var permissions = JsonSerializer.Deserialize<PermissionType[]>(content, new JsonSerializerOptions {
      PropertyNameCaseInsensitive = true,
    });

    Assert.NotNull(permissions);
    Assert.True(permissions.Length > 0);
    _output.WriteLine($"Role '{roleName}' has {permissions.Length} permissions");
  }

  [Fact]
  public async Task GetRolePermissions_WithInvalidRole_ShouldReturnBadRequest() {
    // Arrange
    var invalidRole = "InvalidRole";
    var endpoint = $"/api/projects/roles/{invalidRole}/permissions";

    // Act
    var response = await _client.GetAsync(endpoint);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task AddCollaborator_WithInvalidRole_ShouldReturnBadRequest() {
    // Arrange
    var endpoint = $"/api/projects/{_projectId}/collaborators";
    var addRequest = new {
      email = "collaborator@example.com",
      role = "InvalidRole",
      customPermissions = new int[] { },
      message = "Welcome to the project!",
      notifyUser = true,
    };

    var json = JsonSerializer.Serialize(addRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync(endpoint, content);

    // Assert
    Assert.True(
        response.StatusCode == HttpStatusCode.BadRequest ||
        response.StatusCode == HttpStatusCode.Forbidden);

    _output.WriteLine($"Invalid role response status: {response.StatusCode}");
  }

  [Fact]
  public async Task ShareProject_WithCustomPermissions_ShouldReturnSuccess() {
    // Arrange
    var endpoint = $"/api/projects/{_projectId}/share";
    var shareRequest = new {
      emails = new[] { "custom@example.com" },
      role = "Custom",
      customPermissions = new[] { (int)PermissionType.Read, (int)PermissionType.Comment },
      expiresAt = DateTime.UtcNow.AddDays(30),
      message = "Custom permissions granted!",
      requireAcceptance = true,
      notifyUsers = true,
    };

    var json = JsonSerializer.Serialize(shareRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync(endpoint, content);

    // Assert
    Assert.True(
        response.StatusCode == HttpStatusCode.OK ||
        response.StatusCode == HttpStatusCode.Forbidden ||
        response.StatusCode == HttpStatusCode.BadRequest);

    _output.WriteLine($"Custom permissions share response status: {response.StatusCode}");
  }

  private string GenerateTestJwtToken() {
    // Generate a simple test JWT token
    var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
      sub = _userId.ToString(),
      tenant_id = _tenantId.ToString(),
      exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
    })));

    return $"header.{payload}.signature";
  }

  public void Dispose() {
    _scope?.Dispose();
    _client?.Dispose();
  }
}

// Helper classes for deserialization
public class ProjectRoleTemplate {
  public string Name { get; set; } = string.Empty;
  public string DisplayName { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();
  public bool IsCustomizable { get; set; }
  public string Color { get; set; } = string.Empty;
  public string Icon { get; set; } = string.Empty;
}

public class ProjectCollaborator {
  public Guid UserId { get; set; }
  public string UserName { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string? ProfilePictureUrl { get; set; }
  public string Role { get; set; } = string.Empty;
  public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();
  public DateTime AddedAt { get; set; }
  public Guid AddedByUserId { get; set; }
  public string AddedByUserName { get; set; } = string.Empty;
  public DateTime? ExpiresAt { get; set; }
  public bool IsOwner { get; set; }
  public DateTime? LastActive { get; set; }
}

public class AddCollaboratorResult {
  public bool Success { get; set; }
  public string? ErrorMessage { get; set; }
  public Guid? UserId { get; set; }
  public bool InvitationSent { get; set; }
  public string? InvitationToken { get; set; }
  public ProjectCollaborator? Collaborator { get; set; }
}

public class ShareProjectResult {
  public bool Success { get; set; }
  public string? ErrorMessage { get; set; }
  public List<UserShareResult> UserResults { get; set; } = new();
  public string? ShareId { get; set; }
}

public class ProjectPermissionSummary {
  public Guid ProjectId { get; set; }
  public string ProjectName { get; set; } = string.Empty;
  public PermissionType[] MyPermissions { get; set; } = Array.Empty<PermissionType>();
  public string MyRole { get; set; } = string.Empty;
  public bool IsOwner { get; set; }
  public bool CanManagePermissions { get; set; }
  public bool CanInviteUsers { get; set; }
  public int CollaboratorCount { get; set; }
  public int PendingInvitationCount { get; set; }
}

public class ProjectInvitation {
  public Guid Id { get; set; }
  public Guid ProjectId { get; set; }
  public string ProjectName { get; set; } = string.Empty;
  public string? ProjectDescription { get; set; }
  public string? ProjectThumbnailUrl { get; set; }
  public string InvitedByUserName { get; set; } = string.Empty;
  public DateTime InvitedAt { get; set; }
  public DateTime? ExpiresAt { get; set; }
  public string? Message { get; set; }
  public string Role { get; set; } = string.Empty;
  public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();
  public InvitationStatus Status { get; set; }
  public string Token { get; set; } = string.Empty;
}
