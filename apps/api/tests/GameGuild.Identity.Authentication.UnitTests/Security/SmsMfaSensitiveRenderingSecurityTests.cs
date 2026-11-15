using System.Reflection;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Security;

public sealed class SmsMfaSensitiveRenderingSecurityTests
{
    [Theory]
    [InlineData(nameof(SmsMfaSetupRequest))]
    [InlineData(nameof(SmsMfaChallengeRequest))]
    [InlineData(nameof(StartSmsMfaEnrollmentCommand))]
    [InlineData(nameof(SendSmsMfaChallengeCommand))]
    public void SensitiveSmsInputsRenderOnlyTheirTypeName(string inputType)
    {
        var secret = Guid.NewGuid().ToString("N");
        const string phone = "+15555550123";
        object input = inputType switch
        {
            nameof(SmsMfaSetupRequest) => new SmsMfaSetupRequest { PhoneNumber = phone, Password = secret },
            nameof(SmsMfaChallengeRequest) => new SmsMfaChallengeRequest { MfaToken = secret },
            nameof(StartSmsMfaEnrollmentCommand) => new StartSmsMfaEnrollmentCommand(Guid.NewGuid(), phone, secret),
            nameof(SendSmsMfaChallengeCommand) => new SendSmsMfaChallengeCommand(secret),
            _ => throw new ArgumentOutOfRangeException(nameof(inputType))
        };

        var rendered = input.ToString();
        Assert.NotNull(rendered);
        Assert.Equal(inputType, rendered);
        Assert.DoesNotContain(secret, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(phone, rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void SensitiveSmsFieldsRemainExplicitlyMarkedForAuditRedaction()
    {
        (Type Owner, string Property)[] fields =
        [
            (typeof(SmsMfaSetupRequest), nameof(SmsMfaSetupRequest.PhoneNumber)),
            (typeof(SmsMfaSetupRequest), nameof(SmsMfaSetupRequest.Password)),
            (typeof(SmsMfaChallengeRequest), nameof(SmsMfaChallengeRequest.MfaToken)),
            (typeof(StartSmsMfaEnrollmentCommand), nameof(StartSmsMfaEnrollmentCommand.PhoneNumber)),
            (typeof(StartSmsMfaEnrollmentCommand), nameof(StartSmsMfaEnrollmentCommand.Password)),
            (typeof(SendSmsMfaChallengeCommand), nameof(SendSmsMfaChallengeCommand.MfaToken))
        ];
        foreach (var field in fields)
        {
            Assert.NotNull(field.Owner.GetProperty(field.Property)?
                .GetCustomAttribute<GameGuild.AuditSensitiveAttribute>());
        }
    }
}
