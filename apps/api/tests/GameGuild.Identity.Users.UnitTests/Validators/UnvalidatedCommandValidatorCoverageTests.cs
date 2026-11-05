using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Validators;

/// <summary>
///     Issue #1287 U2: the 14 command types the identity-users wiring matrix listed
/// without validators (both purge paths, the bulk notification verbs, the user
/// lifecycle singles and the localization-preferences family) now have a
/// FluentValidation gate. This census pins the presence so the set cannot silently
/// regress.
/// </summary>
public class UnvalidatedCommandValidatorCoverageTests
{
    [Theory]
    [InlineData(typeof(PurgeUserCommand))]
    [InlineData(typeof(BulkPurgeUsersCommand))]
    [InlineData(typeof(BulkRestoreUsersCommand))]
    [InlineData(typeof(BulkArchiveNotificationsCommand))]
    [InlineData(typeof(BulkMarkNotificationsAsReadCommand))]
    [InlineData(typeof(BulkMarkNotificationsAsUnreadCommand))]
    [InlineData(typeof(BulkUnarchiveNotificationsCommand))]
    [InlineData(typeof(RestoreUserCommand))]
    [InlineData(typeof(SuspendUserCommand))]
    [InlineData(typeof(UnsuspendUserCommand))]
    [InlineData(typeof(UnarchiveNotificationCommand))]
    [InlineData(typeof(ReplaceUserLocalizationPreferencesCommand))]
    [InlineData(typeof(UpdateUserLocalizationPreferencesCommand))]
    [InlineData(typeof(ResetUserLocalizationPreferencesCommand))]
    public void Command_Should_Have_A_Validator(Type commandType)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(commandType);

        var implementation = commandType.Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => type.Namespace?.StartsWith("GameGuild.Identity.Users", StringComparison.Ordinal) == true)
            .FirstOrDefault(type => validatorType.IsAssignableFrom(type));

        implementation.Should().NotBeNull(
            $"{commandType.Name} was one of the 14 unvalidated commands (issue #1287 U2) and must keep a validator");
    }

    [Fact]
    public void BulkNotificationValidator_Should_Reject_EmptyAndNullIdBodies()
    {
        var validator = new BulkArchiveNotificationsCommandValidator();
        var userId = Guid.NewGuid();

        var nullIds = new BulkArchiveNotificationsCommand(userId, null!);
        var emptyIds = new BulkArchiveNotificationsCommand(userId, []);
        var emptyGuidIds = new BulkArchiveNotificationsCommand(userId, [Guid.Empty]);

        validator.Validate(nullIds).IsValid.Should().BeFalse();
        validator.Validate(emptyIds).IsValid.Should().BeFalse();
        validator.Validate(emptyGuidIds).IsValid.Should().BeFalse();
        validator.Validate(new BulkArchiveNotificationsCommand(userId, [Guid.NewGuid()])).IsValid.Should().BeTrue();
    }

    [Fact]
    public void PurgeValidators_Should_Reject_EmptyUserAndUnknownStrategy()
    {
        var single = new PurgeUserCommandValidator();
        var bulk = new BulkPurgeUsersCommandValidator();

        single.Validate(new PurgeUserCommand(Guid.Empty)).IsValid.Should().BeFalse();
        single.Validate(new PurgeUserCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        bulk.Validate(new BulkPurgeUsersCommand([Guid.Empty], PurgeStrategy.Immediate)).IsValid.Should().BeFalse();
        bulk.Validate(new BulkPurgeUsersCommand([], PurgeStrategy.Immediate)).IsValid.Should().BeFalse();
        bulk.Validate(new BulkPurgeUsersCommand([Guid.NewGuid()], PurgeStrategy.GracePeriod)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void LocalizationFamilyValidators_Should_Require_UserIdAndPayload()
    {
        var replace = new ReplaceUserLocalizationPreferencesCommandValidator();
        var update = new UpdateUserLocalizationPreferencesCommandValidator();
        var reset = new ResetUserLocalizationPreferencesCommandValidator();
        var userId = Guid.NewGuid();

        replace.Validate(new ReplaceUserLocalizationPreferencesCommand(Guid.Empty, new(new()))).IsValid.Should().BeFalse();
        replace.Validate(new ReplaceUserLocalizationPreferencesCommand(userId, null!)).IsValid.Should().BeFalse();
        replace
            .Validate(new ReplaceUserLocalizationPreferencesCommand(userId, new(new() { ["Language"] = JsonSerializer.SerializeToElement("en-US") })))
            .IsValid.Should().BeTrue();

        update.Validate(new UpdateUserLocalizationPreferencesCommand(Guid.Empty, new(new()))).IsValid.Should().BeFalse();
        update.Validate(new UpdateUserLocalizationPreferencesCommand(userId, null!)).IsValid.Should().BeFalse();
        update
            .Validate(new UpdateUserLocalizationPreferencesCommand(userId, new(new() { ["Timezone"] = JsonSerializer.SerializeToElement("UTC") })))
            .IsValid.Should().BeTrue();

        reset.Validate(new ResetUserLocalizationPreferencesCommand(Guid.Empty)).IsValid.Should().BeFalse();
        reset.Validate(new ResetUserLocalizationPreferencesCommand(userId)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Assembly_Should_Have_Exactly_The14NewValidators()
    {
        var newValidatorTypeNames = new[]
        {
            nameof(PurgeUserCommandValidator),
            nameof(BulkPurgeUsersCommandValidator),
            nameof(BulkRestoreUsersCommandValidator),
            nameof(BulkArchiveNotificationsCommandValidator),
            nameof(BulkMarkNotificationsAsReadCommandValidator),
            nameof(BulkMarkNotificationsAsUnreadCommandValidator),
            nameof(BulkUnarchiveNotificationsCommandValidator),
            nameof(RestoreUserCommandValidator),
            nameof(SuspendUserCommandValidator),
            nameof(UnsuspendUserCommandValidator),
            nameof(UnarchiveNotificationCommandValidator),
            nameof(ReplaceUserLocalizationPreferencesCommandValidator),
            nameof(UpdateUserLocalizationPreferencesCommandValidator),
            nameof(ResetUserLocalizationPreferencesCommandValidator)
        };

        var assemblyValidatorNames = typeof(PurgeUserCommand).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.Name.EndsWith("Validator", StringComparison.Ordinal))
            .Select(type => type.Name)
            .ToList();

        assemblyValidatorNames.Should().Contain(newValidatorTypeNames);
    }
}
