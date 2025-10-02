using FluentValidation.TestHelper;
using GameGuild.Modules.Permissions.Commands;
using GameGuild.Modules.Permissions.Validators;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Validators;

/// <summary>
/// Unit tests for the GrantTenantPermissionCommandValidator
/// Tests command validation logic, business rules, and error conditions
/// </summary>
public class GrantTenantPermissionCommandValidatorTests
{
    private readonly GrantTenantPermissionCommandValidator _validator;

    public GrantTenantPermissionCommandValidatorTests()
    {
        _validator = new GrantTenantPermissionCommandValidator();
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_ForValidCommand()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { PermissionType.Read, PermissionType.Comment },
            Reason = "Valid reason"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenPermissionsIsEmpty()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = Array.Empty<PermissionType>()
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Permissions)
            .WithErrorMessage("At least one permission must be specified");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenPermissionsIsNull()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = null!
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Permissions);
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenReasonIsTooLong()
    {
        // Arrange
        var longReason = new string('a', 501); // Over 500 characters
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { PermissionType.Read },
            Reason = longReason
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason cannot exceed 500 characters");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenReasonIsNull()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { PermissionType.Read },
            Reason = null
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenReasonIsMaxLength()
    {
        // Arrange
        var maxLengthReason = new string('a', 500); // Exactly 500 characters
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { PermissionType.Read },
            Reason = maxLengthReason
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenExpiresAtIsInPast()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { PermissionType.Read },
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Past date
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt)
            .WithErrorMessage("Expiration date must be in the future");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenExpiresAtIsInFuture()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { PermissionType.Read },
            ExpiresAt = DateTime.UtcNow.AddDays(1) // Future date
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenExpiresAtIsNull()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { PermissionType.Read },
            ExpiresAt = null
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenUserIdIsNull()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = null,
            TenantId = Guid.NewGuid(),
            Permissions = new[] { PermissionType.Read }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenTenantIdIsNull()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = null,
            Permissions = new[] { PermissionType.Read }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    [Theory]
    [InlineData(PermissionType.Read)]
    [InlineData(PermissionType.Comment)]
    [InlineData(PermissionType.Vote)]
    [InlineData(PermissionType.Share)]
    [InlineData(PermissionType.Report)]
    public async Task Validate_ShouldNotHaveError_ForValidPermissionTypes(PermissionType permissionType)
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { permissionType }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Permissions);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_ForMultiplePermissions()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[]
            {
                PermissionType.Read,
                PermissionType.Comment,
                PermissionType.Vote,
                PermissionType.Share
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Permissions);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_ForDuplicatePermissions()
    {
        // Arrange - Include duplicate permission
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[]
            {
                PermissionType.Read,
                PermissionType.Read,  // Duplicate
                PermissionType.Comment
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Permissions);
    }

    [Fact]
    public async Task Validate_ShouldPassAllValidations_ForCompleteValidCommand()
    {
        // Arrange
        var command = new GrantTenantPermissionCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = new[] { PermissionType.Read, PermissionType.Comment },
            Reason = "Valid business reason for granting permissions",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }


}