using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

/// <summary>
/// Capability coverage for cohort authoring/admin mutations (issue #1226 N7): every
/// mutating endpoint carries a resource/content-type permission attribute following the
/// module family's most-protected patterns (CohortSchedulesController's resource
/// permissions, LearningPathController's content-type create), so cohort management is
/// no longer reachable with a bare authenticated identity.
/// </summary>
public class CohortsControllerCapabilityTests
{
    [Theory]
    [InlineData(nameof(CohortsController.UpdateCohort), nameof(PermissionType.Edit))]
    [InlineData(nameof(CohortsController.OpenCohort), nameof(PermissionType.Edit))]
    [InlineData(nameof(CohortsController.CloseCohort), nameof(PermissionType.Edit))]
    [InlineData(nameof(CohortsController.CompleteCohort), nameof(PermissionType.Edit))]
    [InlineData(nameof(CohortsController.CancelCohort), nameof(PermissionType.Edit))]
    [InlineData(nameof(CohortsController.DeleteCohort), nameof(PermissionType.Delete))]
    public void CohortMutations_RequireCohortResourcePermission(string actionName, string permissionName)
    {
        var action = typeof(CohortsController).GetMethod(actionName)!;

        var attribute = action.GetCustomAttributes(true)
            .OfType<IResourcePermissionMarker>()
            .Single();

        attribute.ResourceType.Should().Be(typeof(Cohort));
        attribute.ResourceIdParameterName.Should().Be("id", "cohort mutations address the cohort by route id");
        // PermissionType.Delete is an alias of PermissionType.SoftDelete (both = 26), and
        // a boxed enum's ToString() resolves a duplicated value to the last-declared name,
        // so the pin compares enum values — the required permission, not its alias spelling.
        attribute.RequiredPermission.Should().Be(Enum.Parse<PermissionType>(permissionName));
    }

    [Fact]
    public void CreateCohort_RequiresCohortContentTypeCreatePermission()
    {
        // The course id arrives in the body (no route value the resource attribute could
        // resolve), so the create follows the content-type create pattern used by the
        // module family's other body-carried creates (LearningPath, FeaturedContent).
        var action = typeof(CohortsController).GetMethod(nameof(CohortsController.CreateCohort))!;

        var attribute = action.GetCustomAttributes(true)
            .OfType<IContentTypePermissionMarker>()
            .Single();

        attribute.ResourceType.Should().Be(typeof(Cohort));
        attribute.Permission.ToString().Should().Be(nameof(PermissionType.Create));
    }

    [Fact]
    public void Controller_RemainsAuthorizationRequired()
    {
        typeof(CohortsController).GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Should().ContainSingle();
    }
}
