using GameGuild.Data;
using GameGuild.Modules.Tenant.Dtos;
using GameGuild.Modules.Tenant.Models;
using GameGuild.Modules.Tenant.Services;
using GameGuild.Modules.User.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Modules.Tenant.Unit.Services;

public class TenantDomainServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TenantDomainService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public TenantDomainServiceTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new TenantDomainService(_context);

        // Setup test data
        SetupTestData();
    }

    private void SetupTestData()
    {
        // Create test tenant
        var tenant = new GameGuild.Modules.Tenant.Models.Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Description = "Test tenant for domain tests",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);

        // Create test user
        var user = new User
        {
            Id = _userId,
            Name = "Test User",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        _context.SaveChanges();
    }

    #region Domain Tests

    [Fact]
    public async Task CreateDomainAsync_WithValidData_CreatesDomain()
    {
        // Arrange
        var dto = new CreateTenantDomainDto
        {
            TenantId = _tenantId,
            TopLevelDomain = "example.com",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false
        };

        // Act
        var result = await _service.CreateDomainAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.TopLevelDomain, result.TopLevelDomain);
        Assert.Equal(dto.Subdomain, result.Subdomain);
        Assert.Equal(dto.IsMainDomain, result.IsMainDomain);
        Assert.Equal(dto.IsSecondaryDomain, result.IsSecondaryDomain);
        Assert.Equal(dto.TenantId, result.TenantId);

        // Verify it was saved to database
        var savedDomain = await _context.TenantDomains.FirstOrDefaultAsync(d => d.Id == result.Id);
        Assert.NotNull(savedDomain);
    }

    [Fact]
    public async Task CreateDomainAsync_WithSubdomain_CreatesDomainWithSubdomain()
    {
        // Arrange
        var dto = new CreateTenantDomainDto
        {
            TenantId = _tenantId,
            TopLevelDomain = "university.edu",
            Subdomain = "cs",
            IsMainDomain = false,
            IsSecondaryDomain = true
        };

        // Act
        var result = await _service.CreateDomainAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("university.edu", result.TopLevelDomain);
        Assert.Equal("cs", result.Subdomain);
        Assert.False(result.IsMainDomain);
        Assert.True(result.IsSecondaryDomain);
    }

    [Fact]
    public async Task CreateDomainAsync_DuplicateDomain_ThrowsException()
    {
        // Arrange
        var domain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            TopLevelDomain = "example.com",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TenantDomains.Add(domain);
        await _context.SaveChangesAsync();

        var dto = new CreateTenantDomainDto
        {
            TenantId = _tenantId,
            TopLevelDomain = "example.com",
            Subdomain = null,
            IsMainDomain = false,
            IsSecondaryDomain = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateDomainAsync(dto));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task GetDomainsByTenantAsync_WithExistingDomains_ReturnsCorrectDomains()
    {
        // Arrange
        var domain1 = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            TopLevelDomain = "example.com",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var domain2 = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            TopLevelDomain = "university.edu",
            Subdomain = "cs",
            IsMainDomain = false,
            IsSecondaryDomain = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var otherTenantDomain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TopLevelDomain = "other.com",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TenantDomains.AddRange(domain1, domain2, otherTenantDomain);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDomainsByTenantAsync(_tenantId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, d => d.TopLevelDomain == "example.com");
        Assert.Contains(result, d => d.TopLevelDomain == "university.edu" && d.Subdomain == "cs");
        Assert.DoesNotContain(result, d => d.TopLevelDomain == "other.com");
    }

    [Fact]
    public async Task UpdateDomainAsync_WithValidData_UpdatesDomain()
    {
        // Arrange
        var domain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            TopLevelDomain = "example.com",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TenantDomains.Add(domain);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateTenantDomainDto
        {
            Id = domain.Id,
            IsMainDomain = false,
            IsSecondaryDomain = true
        };

        // Act
        var result = await _service.UpdateDomainAsync(updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsMainDomain);
        Assert.True(result.IsSecondaryDomain);

        // Verify it was updated in database
        var updatedDomain = await _context.TenantDomains.FirstOrDefaultAsync(d => d.Id == domain.Id);
        Assert.NotNull(updatedDomain);
        Assert.False(updatedDomain.IsMainDomain);
        Assert.True(updatedDomain.IsSecondaryDomain);
    }

    [Fact]
    public async Task DeleteDomainAsync_WithValidId_DeletesDomain()
    {
        // Arrange
        var domain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            TopLevelDomain = "example.com",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TenantDomains.Add(domain);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteDomainAsync(domain.Id);

        // Assert
        Assert.True(result);

        // Verify it was deleted from database
        var deletedDomain = await _context.TenantDomains.FirstOrDefaultAsync(d => d.Id == domain.Id);
        Assert.Null(deletedDomain);
    }

    #endregion

    #region User Group Tests

    [Fact]
    public async Task CreateUserGroupAsync_WithValidData_CreatesGroup()
    {
        // Arrange
        var dto = new CreateTenantUserGroupDto
        {
            TenantId = _tenantId,
            Name = "Students",
            Description = "Student group",
            IsDefault = true,
            ParentGroupId = null
        };

        // Act
        var result = await _service.CreateUserGroupAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.IsDefault, result.IsDefault);
        Assert.Equal(dto.TenantId, result.TenantId);
        Assert.Null(result.ParentGroupId);

        // Verify it was saved to database
        var savedGroup = await _context.TenantUserGroups.FirstOrDefaultAsync(g => g.Id == result.Id);
        Assert.NotNull(savedGroup);
    }

    [Fact]
    public async Task CreateUserGroupAsync_WithParentGroup_CreatesSubgroup()
    {
        // Arrange
        var parentGroup = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Faculty",
            Description = "Faculty members",
            IsDefault = false,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TenantUserGroups.Add(parentGroup);
        await _context.SaveChangesAsync();

        var dto = new CreateTenantUserGroupDto
        {
            TenantId = _tenantId,
            Name = "Professors",
            Description = "Professor subgroup",
            IsDefault = false,
            ParentGroupId = parentGroup.Id
        };

        // Act
        var result = await _service.CreateUserGroupAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(parentGroup.Id, result.ParentGroupId);
    }

    [Fact]
    public async Task GetUserGroupsByTenantAsync_WithExistingGroups_ReturnsCorrectGroups()
    {
        // Arrange
        var group1 = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Students",
            Description = "Student group",
            IsDefault = true,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var group2 = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Faculty",
            Description = "Faculty group",
            IsDefault = false,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var otherTenantGroup = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Other Group",
            Description = "Group for different tenant",
            IsDefault = false,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TenantUserGroups.AddRange(group1, group2, otherTenantGroup);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserGroupsByTenantAsync(_tenantId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, g => g.Name == "Students");
        Assert.Contains(result, g => g.Name == "Faculty");
        Assert.DoesNotContain(result, g => g.Name == "Other Group");
    }

    #endregion

    #region Membership Tests

    [Fact]
    public async Task AssignUserToGroupAsync_WithValidData_CreatesMembership()
    {
        // Arrange
        var group = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Students",
            Description = "Student group",
            IsDefault = true,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TenantUserGroups.Add(group);
        await _context.SaveChangesAsync();

        var dto = new AssignUserToGroupDto
        {
            UserId = _userId,
            GroupId = group.Id,
            IsAutoAssigned = false
        };

        // Act
        var result = await _service.AssignUserToGroupAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.UserId, result.UserId);
        Assert.Equal(dto.GroupId, result.GroupId);
        Assert.Equal(dto.IsAutoAssigned, result.IsAutoAssigned);

        // Verify it was saved to database
        var savedMembership = await _context.TenantUserGroupMemberships
            .FirstOrDefaultAsync(m => m.Id == result.Id);
        Assert.NotNull(savedMembership);
    }

    [Fact]
    public async Task AssignUserToGroupAsync_DuplicateAssignment_ThrowsException()
    {
        // Arrange
        var group = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Students",
            Description = "Student group",
            IsDefault = true,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TenantUserGroups.Add(group);

        var existingMembership = new TenantUserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            GroupId = group.Id,
            IsAutoAssigned = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TenantUserGroupMemberships.Add(existingMembership);
        await _context.SaveChangesAsync();

        var dto = new AssignUserToGroupDto
        {
            UserId = _userId,
            GroupId = group.Id,
            IsAutoAssigned = false
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AssignUserToGroupAsync(dto));
        Assert.Contains("already assigned", exception.Message);
    }

    [Fact]
    public async Task GetUserGroupMembershipsAsync_WithExistingMemberships_ReturnsCorrectMemberships()
    {
        // Arrange
        var group1 = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Students",
            Description = "Student group",
            IsDefault = true,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var group2 = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Faculty",
            Description = "Faculty group",
            IsDefault = false,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TenantUserGroups.AddRange(group1, group2);

        var membership1 = new TenantUserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            GroupId = group1.Id,
            IsAutoAssigned = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var membership2 = new TenantUserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            GroupId = group2.Id,
            IsAutoAssigned = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var otherUserMembership = new TenantUserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GroupId = group1.Id,
            IsAutoAssigned = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TenantUserGroupMemberships.AddRange(membership1, membership2, otherUserMembership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserGroupMembershipsAsync(_userId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, m => m.GroupId == group1.Id);
        Assert.Contains(result, m => m.GroupId == group2.Id);
        Assert.DoesNotContain(result, m => m.UserId != _userId);
    }

    #endregion

    #region Auto-Assignment Tests

    [Fact]
    public async Task GetAutoAssignmentGroupAsync_WithMatchingDomain_ReturnsCorrectGroup()
    {
        // Arrange
        var domain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            TopLevelDomain = "university.edu",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var group = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Students",
            Description = "Student group",
            IsDefault = true,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TenantDomains.Add(domain);
        _context.TenantUserGroups.Add(group);

        // Link domain to group
        domain.AutoAssignmentGroups = new List<TenantUserGroup> { group };
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAutoAssignmentGroupAsync("test@university.edu");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.Id);
        Assert.Equal("Students", result.Name);
    }

    [Fact]
    public async Task GetAutoAssignmentGroupAsync_WithSubdomain_ReturnsCorrectGroup()
    {
        // Arrange
        var domain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            TopLevelDomain = "university.edu",
            Subdomain = "cs",
            IsMainDomain = false,
            IsSecondaryDomain = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var group = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "CS Students",
            Description = "Computer Science students",
            IsDefault = false,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TenantDomains.Add(domain);
        _context.TenantUserGroups.Add(group);

        // Link domain to group
        domain.AutoAssignmentGroups = new List<TenantUserGroup> { group };
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAutoAssignmentGroupAsync("student@cs.university.edu");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.Id);
        Assert.Equal("CS Students", result.Name);
    }

    [Fact]
    public async Task GetAutoAssignmentGroupAsync_WithNoMatchingDomain_ReturnsNull()
    {
        // Arrange - no domains in database

        // Act
        var result = await _service.GetAutoAssignmentGroupAsync("test@nonexistent.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AutoAssignUserToGroupAsync_WithValidEmail_AssignsUser()
    {
        // Arrange
        var domain = new TenantDomain
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            TopLevelDomain = "university.edu",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var group = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Students",
            Description = "Student group",
            IsDefault = true,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TenantDomains.Add(domain);
        _context.TenantUserGroups.Add(group);

        // Link domain to group
        domain.AutoAssignmentGroups = new List<TenantUserGroup> { group };
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AutoAssignUserToGroupAsync(_userId, "test@university.edu");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal(group.Id, result.GroupId);
        Assert.True(result.IsAutoAssigned);

        // Verify it was saved to database
        var savedMembership = await _context.TenantUserGroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == _userId && m.GroupId == group.Id);
        Assert.NotNull(savedMembership);
        Assert.True(savedMembership.IsAutoAssigned);
    }

    [Fact]
    public async Task AutoAssignUserToGroupAsync_WithNoMatchingDomain_ReturnsNull()
    {
        // Arrange - no domains in database

        // Act
        var result = await _service.AutoAssignUserToGroupAsync(_userId, "test@nonexistent.com");

        // Assert
        Assert.Null(result);

        // Verify no membership was created
        var membership = await _context.TenantUserGroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == _userId);
        Assert.Null(membership);
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task GetUsersByGroupAsync_WithExistingMemberships_ReturnsCorrectUsers()
    {
        // Arrange
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User 2",
            Email = "test2@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user2);

        var group = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Students",
            Description = "Student group",
            IsDefault = true,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TenantUserGroups.Add(group);

        var membership1 = new TenantUserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            GroupId = group.Id,
            IsAutoAssigned = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var membership2 = new TenantUserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            GroupId = group.Id,
            IsAutoAssigned = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TenantUserGroupMemberships.AddRange(membership1, membership2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUsersByGroupAsync(group.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, u => u.Id == _userId);
        Assert.Contains(result, u => u.Id == user2.Id);
    }

    [Fact]
    public async Task GetGroupsByUserAsync_WithExistingMemberships_ReturnsCorrectGroups()
    {
        // Arrange
        var group1 = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Students",
            Description = "Student group",
            IsDefault = true,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var group2 = new TenantUserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Faculty",
            Description = "Faculty group",
            IsDefault = false,
            ParentGroupId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TenantUserGroups.AddRange(group1, group2);

        var membership1 = new TenantUserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            GroupId = group1.Id,
            IsAutoAssigned = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var membership2 = new TenantUserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            GroupId = group2.Id,
            IsAutoAssigned = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TenantUserGroupMemberships.AddRange(membership1, membership2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetGroupsByUserAsync(_userId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, g => g.Id == group1.Id);
        Assert.Contains(result, g => g.Id == group2.Id);
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}
