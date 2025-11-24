using FluentAssertions;
using GameGuild.Permissions;
using GameGuild.Permissions.Entities;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Entities;

/// <summary>
/// Unit tests for PermissionTemplate entity
/// </summary>
public class PermissionTemplateTests
{
    [Fact]
    public void PermissionTemplate_Should_Have_Required_Properties()
    {
        // Arrange
        var name = "Content Editor";
        var description = "Can edit all content";
        var permissions = new[] { PermissionType.Read, PermissionType.Edit };
        var module = ModuleType.ContentManagement;
        var category = "Content";

        // Act
        var template = new PermissionTemplate
        {
            Name = name,
            Description = description,
            Permissions = permissions,
            Module = module,
            IsSystemTemplate = true,
            IsActive = true,
            Category = category
        };

        // Assert
        template.Name.Should().Be(name);
        template.Description.Should().Be(description);
        template.Permissions.Should().BeEquivalentTo(permissions);
        template.Module.Should().Be(module);
        template.IsSystemTemplate.Should().BeTrue();
        template.IsActive.Should().BeTrue();
        template.Category.Should().Be(category);
    }

    [Fact]
    public void PermissionTemplate_Should_Default_Permissions_To_Empty_Array()
    {
        // Arrange & Act
        var template = new PermissionTemplate();

        // Assert
        template.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void PermissionTemplate_Should_Default_IsActive_To_True()
    {
        // Arrange & Act
        var template = new PermissionTemplate();

        // Assert
        template.IsActive.Should().BeTrue();
    }
}
