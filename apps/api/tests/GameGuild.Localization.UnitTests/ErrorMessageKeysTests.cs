using FluentAssertions;
using Xunit;

namespace GameGuild.Localization.UnitTests;

public class ErrorMessageKeysTests
{
    [Theory]
    [InlineData(nameof(ErrorMessageKeys.Validation.Required), "validation.required")]
    [InlineData(nameof(ErrorMessageKeys.Validation.MinLength), "validation.minLength")]
    [InlineData(nameof(ErrorMessageKeys.Validation.MaxLength), "validation.maxLength")]
    [InlineData(nameof(ErrorMessageKeys.Validation.Email), "validation.email")]
    [InlineData(nameof(ErrorMessageKeys.Validation.Range), "validation.range")]
    [InlineData(nameof(ErrorMessageKeys.Validation.Regex), "validation.regex")]
    [InlineData(nameof(ErrorMessageKeys.Validation.Comparison), "validation.comparison")]
    [InlineData(nameof(ErrorMessageKeys.Validation.Unique), "validation.unique")]
    public void Validation_Keys_ShouldHaveCorrectValues(string fieldName, string expected)
    {
        var value = typeof(ErrorMessageKeys.Validation).GetField(fieldName)!.GetRawConstantValue() as string;
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData(nameof(ErrorMessageKeys.Auth.Unauthorized), "auth.unauthorized")]
    [InlineData(nameof(ErrorMessageKeys.Auth.Forbidden), "auth.forbidden")]
    [InlineData(nameof(ErrorMessageKeys.Auth.TokenExpired), "auth.tokenExpired")]
    [InlineData(nameof(ErrorMessageKeys.Auth.InvalidCredentials), "auth.invalidCredentials")]
    [InlineData(nameof(ErrorMessageKeys.Auth.AccountLocked), "auth.accountLocked")]
    [InlineData(nameof(ErrorMessageKeys.Auth.AccountDisabled), "auth.accountDisabled")]
    [InlineData(nameof(ErrorMessageKeys.Auth.SessionExpired), "auth.sessionExpired")]
    public void Auth_Keys_ShouldHaveCorrectValues(string fieldName, string expected)
    {
        var value = typeof(ErrorMessageKeys.Auth).GetField(fieldName)!.GetRawConstantValue() as string;
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData(nameof(ErrorMessageKeys.Resource.NotFound), "resource.notFound")]
    [InlineData(nameof(ErrorMessageKeys.Resource.AlreadyExists), "resource.alreadyExists")]
    [InlineData(nameof(ErrorMessageKeys.Resource.Conflict), "resource.conflict")]
    [InlineData(nameof(ErrorMessageKeys.Resource.Deleted), "resource.deleted")]
    public void Resource_Keys_ShouldHaveCorrectValues(string fieldName, string expected)
    {
        var value = typeof(ErrorMessageKeys.Resource).GetField(fieldName)!.GetRawConstantValue() as string;
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData(nameof(ErrorMessageKeys.Quota.Exceeded), "quota.exceeded")]
    [InlineData(nameof(ErrorMessageKeys.Quota.NearLimit), "quota.nearLimit")]
    [InlineData(nameof(ErrorMessageKeys.Quota.StorageFull), "quota.storageFull")]
    public void Quota_Keys_ShouldHaveCorrectValues(string fieldName, string expected)
    {
        var value = typeof(ErrorMessageKeys.Quota).GetField(fieldName)!.GetRawConstantValue() as string;
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData(nameof(ErrorMessageKeys.System.InternalError), "system.internalError")]
    [InlineData(nameof(ErrorMessageKeys.System.ServiceUnavailable), "system.serviceUnavailable")]
    [InlineData(nameof(ErrorMessageKeys.System.MaintenanceMode), "system.maintenanceMode")]
    [InlineData(nameof(ErrorMessageKeys.System.RateLimited), "system.rateLimited")]
    public void System_Keys_ShouldHaveCorrectValues(string fieldName, string expected)
    {
        var value = typeof(ErrorMessageKeys.System).GetField(fieldName)!.GetRawConstantValue() as string;
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData(nameof(ErrorMessageKeys.Asset.VirusDetected), "asset.virusDetected")]
    [InlineData(nameof(ErrorMessageKeys.Asset.ModerationRejected), "asset.moderationRejected")]
    [InlineData(nameof(ErrorMessageKeys.Asset.ContentWarning), "asset.contentWarning")]
    [InlineData(nameof(ErrorMessageKeys.Asset.TokenExpired), "asset.tokenExpired")]
    [InlineData(nameof(ErrorMessageKeys.Asset.TokenInvalid), "asset.tokenInvalid")]
    [InlineData(nameof(ErrorMessageKeys.Asset.DownloadLimitExceeded), "asset.downloadLimitExceeded")]
    public void Asset_Keys_ShouldHaveCorrectValues(string fieldName, string expected)
    {
        var value = typeof(ErrorMessageKeys.Asset).GetField(fieldName)!.GetRawConstantValue() as string;
        value.Should().Be(expected);
    }

    [Fact]
    public void AllKeys_ShouldFollowDotNotation()
    {
        var categories = new[]
        {
            typeof(ErrorMessageKeys.Validation),
            typeof(ErrorMessageKeys.Auth),
            typeof(ErrorMessageKeys.Resource),
            typeof(ErrorMessageKeys.Quota),
            typeof(ErrorMessageKeys.System),
            typeof(ErrorMessageKeys.Asset)
        };

        foreach (var category in categories)
        {
            foreach (var field in category.GetFields())
            {
                var value = field.GetRawConstantValue() as string;
                value.Should().NotBeNullOrEmpty();
                value.Should().Contain(".", because: $"key '{field.Name}' should follow category.key format");
            }
        }
    }

    [Fact]
    public void AllKeys_ShouldBeUnique()
    {
        var allKeys = new List<string>();
        var categories = new[]
        {
            typeof(ErrorMessageKeys.Validation),
            typeof(ErrorMessageKeys.Auth),
            typeof(ErrorMessageKeys.Resource),
            typeof(ErrorMessageKeys.Quota),
            typeof(ErrorMessageKeys.System),
            typeof(ErrorMessageKeys.Asset)
        };

        foreach (var category in categories)
        {
            foreach (var field in category.GetFields())
            {
                allKeys.Add((string)field.GetRawConstantValue()!);
            }
        }

        allKeys.Should().OnlyHaveUniqueItems();
    }
}
