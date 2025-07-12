using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Tenants;
using GameGuild.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserModel = GameGuild.Modules.Users.User;


namespace GameGuild.Tests.Modules.Tenants.E2E;

public class TenantDomainSystemE2ETests : IClassFixture<WebApplicationFactory<Program>> {
  private readonly WebApplicationFactory<Program> _factory;
  private readonly HttpClient _client;

  public TenantDomainSystemE2ETests(WebApplicationFactory<Program> factory) {
    // Use a unique database name for this test class to avoid interference
    var uniqueDbName = $"TenantDomainSystemE2E_{Guid.NewGuid()}";
    _factory = IntegrationTestHelper.GetTestFactory(uniqueDbName);
    _client = _factory.CreateClient();
  }

  [Fact]
  public async Task CompleteUniversityScenario_CreateDomainGroupsAndAutoAssign_WorksEndToEnd() {
    // Clear any previous authorization state
    ClearAuthorizationHeader();

    // Arrange - Setup University tenant and users
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Ensure clean database
    if (InMemoryDatabaseFacadeExtensions.IsInMemory(db.Database)) {
      await db.Database.EnsureDeletedAsync();
      await db.Database.EnsureCreatedAsync();
    }

    var tenantId = Guid.NewGuid();

    var tenant = new Tenant {
      Id = tenantId,
      Name = "University of Technology",
      Description = "A technology university",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    db.Tenants.Add(tenant);

    // Create test users
    var studentUser = new UserModel {
      Id = Guid.NewGuid(),
      Name = "John Student",
      Email = "john@university.edu",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var csStudentUser = new UserModel {
      Id = Guid.NewGuid(),
      Name = "Jane CS Student",
      Email = "jane@cs.university.edu",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var professorUser = new UserModel {
      Id = Guid.NewGuid(),
      Name = "Dr. Professor",
      Email = "prof@faculty.university.edu",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var outsideUser = new UserModel {
      Id = Guid.NewGuid(),
      Name = "Outside User",
      Email = "outside@external.com",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    db.Users.AddRange(studentUser, csStudentUser, professorUser, outsideUser);
    await db.SaveChangesAsync();

    // Debug: Verify that data was saved properly
    var savedTenant = await db.Tenants.FindAsync(tenantId);
    var savedUser = await db.Users.FindAsync(studentUser.Id);
    Console.WriteLine($"Saved tenant: {savedTenant?.Name}, Saved user: {savedUser?.Name}");

    // Setup authentication for the test
    await SetupAuthentication(tenantId, studentUser.Id, db);

    // Step 1: Create main university domain
    var createMainDomainDto = new CreateTenantDomainDto {
      TenantId = tenantId,
      TopLevelDomain = "university.edu",
      Subdomain = null,
      IsMainDomain = true,
      IsSecondaryDomain = false
    };

    var json = JsonSerializer.Serialize(createMainDomainDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    var response = await _client.PostAsync("/api/tenant-domains", content);

    // Debug: Log response details
    var responseContent = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Create domain response: Status={response.StatusCode}, Content={responseContent}");

    if (response.StatusCode != HttpStatusCode.Created) throw new Exception($"Domain creation failed. Status: {response.StatusCode}, Content: {responseContent}");

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var mainDomainResult = JsonSerializer.Deserialize<TenantDomain>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(mainDomainResult);

    // Step 2: Create CS subdomain
    var createCSDomainDto = new CreateTenantDomainDto {
      TenantId = tenantId,
      TopLevelDomain = "university.edu",
      Subdomain = "cs",
      IsMainDomain = false,
      IsSecondaryDomain = true
    };

    json = JsonSerializer.Serialize(createCSDomainDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var csDomainResult = JsonSerializer.Deserialize<TenantDomain>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(csDomainResult);

    // Step 3: Create Faculty subdomain
    var createFacultyDomainDto = new CreateTenantDomainDto {
      TenantId = tenantId,
      TopLevelDomain = "university.edu",
      Subdomain = "faculty",
      IsMainDomain = false,
      IsSecondaryDomain = true
    };

    json = JsonSerializer.Serialize(createFacultyDomainDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var facultyDomainResult = JsonSerializer.Deserialize<TenantDomain>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(facultyDomainResult);

    // Step 4: Create user groups
    // General Students group (default for main domain)
    var createStudentsGroupDto = new CreateTenantUserGroupDto {
      TenantId = tenantId,
      Name = "Students",
      Description = "General student body",
      IsDefault = true,
      ParentGroupId = null
    };

    json = JsonSerializer.Serialize(createStudentsGroupDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/user-groups", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var studentsGroupResult = JsonSerializer.Deserialize<TenantUserGroupDto>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(studentsGroupResult);

    // CS Students group (for CS subdomain)
    var createCSGroupDto = new CreateTenantUserGroupDto {
      TenantId = tenantId,
      Name = "CS Students",
      Description = "Computer Science students",
      IsDefault = false,
      ParentGroupId = studentsGroupResult.Id
    };

    json = JsonSerializer.Serialize(createCSGroupDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/user-groups", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var csGroupResult = JsonSerializer.Deserialize<TenantUserGroupDto>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(csGroupResult);

    // Faculty group (for faculty subdomain)
    var createFacultyGroupDto = new CreateTenantUserGroupDto {
      TenantId = tenantId,
      Name = "Faculty",
      Description = "Faculty members",
      IsDefault = false,
      ParentGroupId = null
    };

    json = JsonSerializer.Serialize(createFacultyGroupDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/user-groups", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var facultyGroupResult = JsonSerializer.Deserialize<TenantUserGroupDto>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(facultyGroupResult);

    // Step 5: Setup auto-assignment rules (manually in database for this test)
    // In a real scenario, this would be done through admin interface
    var mainDomain = await db.TenantDomains.FirstAsync(d => d.Id == mainDomainResult.Id);
    var csDomain = await db.TenantDomains.FirstAsync(d => d.Id == csDomainResult.Id);
    var facultyDomain = await db.TenantDomains.FirstAsync(d => d.Id == facultyDomainResult.Id);

    var studentsGroup = await db.TenantUserGroups.FirstAsync(g => g.Id == studentsGroupResult.Id);
    var csGroup = await db.TenantUserGroups.FirstAsync(g => g.Id == csGroupResult.Id);
    var facultyGroup = await db.TenantUserGroups.FirstAsync(g => g.Id == facultyGroupResult.Id);

    // Link domains to groups for auto-assignment
    mainDomain.UserGroupId = studentsGroup.Id;
    csDomain.UserGroupId = csGroup.Id;
    facultyDomain.UserGroupId = facultyGroup.Id;

    db.UpdateRange(mainDomain, csDomain, facultyDomain);
    await db.SaveChangesAsync();

    // Step 6: Test auto-assignment for general student
    var autoAssignStudentDto = new AutoAssignUserDto { UserId = studentUser.Id, Email = studentUser.Email };

    json = JsonSerializer.Serialize(autoAssignStudentDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/auto-assign", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var studentMembershipResult = JsonSerializer.Deserialize<TenantUserGroupMembershipDto>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(studentMembershipResult);
    Assert.Equal(studentUser.Id, studentMembershipResult.UserId);
    Assert.Equal(studentsGroup.Id, studentMembershipResult.GroupId);
    Assert.True(studentMembershipResult.IsAutoAssigned);

    // Step 7: Test auto-assignment for CS student
    var autoAssignCSStudentDto = new AutoAssignUserDto { UserId = csStudentUser.Id, Email = csStudentUser.Email };

    json = JsonSerializer.Serialize(autoAssignCSStudentDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/auto-assign", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var csMembershipResult = JsonSerializer.Deserialize<TenantUserGroupMembershipDto>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(csMembershipResult);
    Assert.Equal(csStudentUser.Id, csMembershipResult.UserId);
    Assert.Equal(csGroup.Id, csMembershipResult.GroupId);
    Assert.True(csMembershipResult.IsAutoAssigned);

    // Step 8: Test auto-assignment for faculty
    var autoAssignFacultyDto = new AutoAssignUserDto { UserId = professorUser.Id, Email = professorUser.Email };

    json = JsonSerializer.Serialize(autoAssignFacultyDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/auto-assign", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var facultyMembershipResult = JsonSerializer.Deserialize<TenantUserGroupMembershipDto>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(facultyMembershipResult);
    Assert.Equal(professorUser.Id, facultyMembershipResult.UserId);
    Assert.Equal(facultyGroup.Id, facultyMembershipResult.GroupId);
    Assert.True(facultyMembershipResult.IsAutoAssigned);

    // Step 9: Test auto-assignment for outside user (should fail)
    var autoAssignOutsideDto = new AutoAssignUserDto { UserId = outsideUser.Id, Email = outsideUser.Email };

    json = JsonSerializer.Serialize(autoAssignOutsideDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/auto-assign", content);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    // Step 10: Verify group memberships
    // Check Students group has the general student
    response = await _client.GetAsync($"/api/tenant-domains/groups/{studentsGroup.Id}/users");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var studentsGroupUsers = JsonSerializer.Deserialize<List<UserDto>>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(studentsGroupUsers);
    Assert.Single(studentsGroupUsers);
    Assert.Contains(studentsGroupUsers, u => u.Id == studentUser.Id);

    // Check CS group has the CS student
    response = await _client.GetAsync($"/api/tenant-domains/groups/{csGroup.Id}/users");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var csGroupUsers = JsonSerializer.Deserialize<List<UserDto>>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(csGroupUsers);
    Assert.Single(csGroupUsers);
    Assert.Contains(csGroupUsers, u => u.Id == csStudentUser.Id);

    // Check Faculty group has the professor
    response = await _client.GetAsync($"/api/tenant-domains/groups/{facultyGroup.Id}/users");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var facultyGroupUsers = JsonSerializer.Deserialize<List<UserDto>>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(facultyGroupUsers);
    Assert.Single(facultyGroupUsers);
    Assert.Contains(facultyGroupUsers, u => u.Id == professorUser.Id);

    // Step 11: Test querying user groups
    // Check CS student's groups
    response = await _client.GetAsync($"/api/tenant-domains/users/{csStudentUser.Id}/groups");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var csStudentGroups = JsonSerializer.Deserialize<List<TenantUserGroupDto>>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(csStudentGroups);
    Assert.Single(csStudentGroups);
    Assert.Contains(csStudentGroups, g => g.Id == csGroup.Id);

    // Step 12: Test manual assignment (adding professor to both faculty and students groups)
    var manualAssignDto = new AddUserToGroupDto { UserId = professorUser.Id, UserGroupId = studentsGroup.Id, IsAutoAssigned = false };

    json = JsonSerializer.Serialize(manualAssignDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/memberships", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var manualMembershipResult = JsonSerializer.Deserialize<TenantUserGroupMembershipDto>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(manualMembershipResult);
    Assert.Equal(professorUser.Id, manualMembershipResult.UserId);
    Assert.Equal(studentsGroup.Id, manualMembershipResult.GroupId);
    Assert.False(manualMembershipResult.IsAutoAssigned);

    // Verify professor is now in both groups
    response = await _client.GetAsync($"/api/tenant-domains/users/{professorUser.Id}/groups");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var professorGroups = JsonSerializer.Deserialize<List<TenantUserGroupDto>>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(professorGroups);
    Assert.Equal(2, professorGroups.Count);
    Assert.Contains(professorGroups, g => g.Id == facultyGroup.Id);
    Assert.Contains(professorGroups, g => g.Id == studentsGroup.Id);

    // Step 13: Test getting all domains for tenant
    response = await _client.GetAsync($"/api/tenant-domains/tenant/{tenantId}");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var tenantDomains = JsonSerializer.Deserialize<List<TenantDomain>>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(tenantDomains);
    Assert.Equal(3, tenantDomains.Count);
    Assert.Contains(tenantDomains, d => d is { TopLevelDomain: "university.edu", Subdomain: null });
    Assert.Contains(tenantDomains, d => d is { TopLevelDomain: "university.edu", Subdomain: "cs" });
    Assert.Contains(tenantDomains, d => d is { TopLevelDomain: "university.edu", Subdomain: "faculty" });

    // Step 14: Test getting all groups for tenant
    response = await _client.GetAsync($"/api/tenant-domains/user-groups/tenant/{tenantId}");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var tenantGroups = JsonSerializer.Deserialize<List<TenantUserGroupDto>>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(tenantGroups);
    Assert.Equal(3, tenantGroups.Count);
    Assert.Contains(tenantGroups, g => g.Name == "Students");
    Assert.Contains(tenantGroups, g => g.Name == "CS Students");
    Assert.Contains(tenantGroups, g => g.Name == "Faculty");
  }

  [Fact]
  public async Task CorporateScenario_MultiDomainWithDepartments_WorksEndToEnd() {
    // Clear any previous authorization state
    ClearAuthorizationHeader();

    // Arrange - Setup Corporate tenant
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Ensure clean database
    if (InMemoryDatabaseFacadeExtensions.IsInMemory(db.Database)) {
      await db.Database.EnsureDeletedAsync();
      await db.Database.EnsureCreatedAsync();
    }

    var tenantId = Guid.NewGuid();

    var tenant = new Tenant {
      Id = tenantId,
      Name = "TechCorp Inc",
      Description = "A technology corporation",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    db.Tenants.Add(tenant);

    // Create test users
    var devUser = new UserModel {
      Id = Guid.NewGuid(),
      Name = "Dev User",
      Email = "dev@techcorp.com",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var hrUser = new UserModel {
      Id = Guid.NewGuid(),
      Name = "HR User",
      Email = "hr@techcorp.com",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var contractorUser = new UserModel {
      Id = Guid.NewGuid(),
      Name = "Contractor User",
      Email = "contractor@partners.techcorp.com",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    db.Users.AddRange(devUser, hrUser, contractorUser);
    await db.SaveChangesAsync();

    // Setup authentication for the test
    await SetupAuthentication(tenantId, devUser.Id, db);

    // Step 1: Create main corporate domain
    var createMainDomainDto = new CreateTenantDomainDto {
      TenantId = tenantId,
      TopLevelDomain = "techcorp.com",
      Subdomain = null,
      IsMainDomain = true,
      IsSecondaryDomain = false
    };

    var json = JsonSerializer.Serialize(createMainDomainDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    var response = await _client.PostAsync("/api/tenant-domains", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    // Step 2: Create partners domain (secondary domain)
    var createPartnersDomainDto = new CreateTenantDomainDto {
      TenantId = tenantId,
      TopLevelDomain = "techcorp.com",
      Subdomain = "partners",
      IsMainDomain = false,
      IsSecondaryDomain = true
    };

    json = JsonSerializer.Serialize(createPartnersDomainDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    // Step 3: Create department groups
    var createEmployeesGroupDto = new CreateTenantUserGroupDto {
      TenantId = tenantId,
      Name = "Employees",
      Description = "All employees",
      IsDefault = true,
      ParentGroupId = null
    };

    json = JsonSerializer.Serialize(createEmployeesGroupDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/user-groups", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var employeesGroupResult = JsonSerializer.Deserialize<TenantUserGroupDto>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    var createContractorsGroupDto = new CreateTenantUserGroupDto {
      TenantId = tenantId,
      Name = "Contractors",
      Description = "External contractors",
      IsDefault = false,
      ParentGroupId = null
    };

    json = JsonSerializer.Serialize(createContractorsGroupDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/user-groups", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var contractorsGroupResult = JsonSerializer.Deserialize<TenantUserGroupDto>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    // Step 4: Test auto-assignment based on email domains
    // Setup auto-assignment rules manually in database
    var mainDomain = await db.TenantDomains.FirstAsync(d =>
                                                         d.TenantId == tenantId && d.TopLevelDomain == "techcorp.com" && d.Subdomain == null
                     );
    var partnersDomain = await db.TenantDomains.FirstAsync(d => d.TenantId == tenantId && d.Subdomain == "partners");

    var employeesGroup = await db.TenantUserGroups.FirstAsync(g => g.Id == employeesGroupResult!.Id);
    var contractorsGroup = await db.TenantUserGroups.FirstAsync(g => g.Id == contractorsGroupResult!.Id);

    mainDomain.UserGroupId = employeesGroup.Id;
    partnersDomain.UserGroupId = contractorsGroup.Id;

    db.UpdateRange(mainDomain, partnersDomain);
    await db.SaveChangesAsync();

    // Test employee auto-assignment
    var autoAssignDevDto = new AutoAssignUserDto { UserId = devUser.Id, Email = devUser.Email };

    json = JsonSerializer.Serialize(autoAssignDevDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/auto-assign", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var autoAssignHRDto = new AutoAssignUserDto { UserId = hrUser.Id, Email = hrUser.Email };

    json = JsonSerializer.Serialize(autoAssignHRDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/auto-assign", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    // Test contractor auto-assignment
    var autoAssignContractorDto = new AutoAssignUserDto { UserId = contractorUser.Id, Email = contractorUser.Email };

    json = JsonSerializer.Serialize(autoAssignContractorDto);
    content = new StringContent(json, Encoding.UTF8, "application/json");
    response = await _client.PostAsync("/api/tenant-domains/auto-assign", content);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    // Step 5: Verify group assignments
    // Check Employees group has both dev and HR users
    response = await _client.GetAsync($"/api/tenant-domains/groups/{employeesGroup.Id}/users");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var employeesGroupUsers = JsonSerializer.Deserialize<List<UserDto>>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(employeesGroupUsers);
    Assert.Equal(2, employeesGroupUsers.Count);
    Assert.Contains(employeesGroupUsers, u => u.Id == devUser.Id);
    Assert.Contains(employeesGroupUsers, u => u.Id == hrUser.Id);

    // Check Contractors group has contractor user
    response = await _client.GetAsync($"/api/tenant-domains/groups/{contractorsGroup.Id}/users");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var contractorsGroupUsers = JsonSerializer.Deserialize<List<UserDto>>(
      await response.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(contractorsGroupUsers);
    Assert.Single(contractorsGroupUsers);
    Assert.Contains(contractorsGroupUsers, u => u.Id == contractorUser.Id);
  }

  private async Task SetupAuthentication(Guid tenantId, Guid userId, ApplicationDbContext db) {
    using var scope = _factory.Services.CreateScope();

    // Get the user and tenant entities
    var user = await db.Users.FirstAsync(u => u.Id == userId);
    var tenant = await db.Tenants.FirstAsync(t => t.Id == tenantId);

    // Grant permissions for TenantDomain operations
    var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService.GrantContentTypePermissionAsync(
      user.Id,
      tenant.Id,
      "TenantDomain",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );

    // Grant permissions for TenantUserGroup operations
    await permissionService.GrantContentTypePermissionAsync(
      user.Id,
      tenant.Id,
      "TenantUserGroup",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );

    // Grant permissions for TenantUserGroupMembership operations
    await permissionService.GrantContentTypePermissionAsync(
      user.Id,
      tenant.Id,
      "TenantUserGroupMembership",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );

    // Generate JWT token
    var token = CreateJwtTokenForUser(user, tenant, scope);

    // Set authorization header
    SetAuthorizationHeader(token);
  }

  private static string CreateJwtTokenForUser(UserModel user, Tenant tenant, IServiceScope scope) {
    var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

    var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };

    var roles = new[] { "User" };
    var additionalClaims = new[] { new Claim("tenant_id", tenant.Id.ToString()) };

    return jwtService.GenerateAccessToken(userDto, roles, additionalClaims);
  }

  private void ClearAuthorizationHeader() { _client.DefaultRequestHeaders.Authorization = null; }

  private void SetAuthorizationHeader(string token) {
    // Clear any existing authorization header first
    _client.DefaultRequestHeaders.Authorization = null;
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
  }
}
