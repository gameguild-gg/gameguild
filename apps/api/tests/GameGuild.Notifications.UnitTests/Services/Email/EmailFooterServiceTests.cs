using GameGuild.Notifications.Services.Email;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Notifications.UnitTests.Services.Email;

public sealed class EmailFooterServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static EmailFooterService CreateSubject(
        IUnsubscribeTokenService? tokenService = null,
        IConfiguration? configuration = null)
    {
        tokenService ??= new UnsubscribeTokenService(new EphemeralDataProtectionProvider());
        configuration ??= new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["App:BaseUrl"] = "https://app.example.com" }).Build();
        return new EmailFooterService(tokenService, configuration);
    }

    private static Notification SuppressibleNotification(Guid? recipientId = null) =>
        Notification.Create(
            recipientId ?? UserId,
            NotificationType.MonthlyStatement,
            NotificationChannel.Email,
            "Statement",
            "Body",
            recipientEmail: "member@example.com");

    private static Notification NullRecipientNotification() =>
        Notification.Create(
            null,
            NotificationType.MonthlyStatement,
            NotificationChannel.Email,
            "Statement",
            "Body",
            recipientEmail: "member@example.com");

    [Fact]
    public void Build_For_Suppressible_Type_With_Recipient_Returns_Footer_With_Valid_Token_And_Manage_Link()
    {
        var tokenService = new UnsubscribeTokenService(new EphemeralDataProtectionProvider());
        var subject = CreateSubject(tokenService);
        var notification = SuppressibleNotification();

        var footer = subject.Build(notification);

        footer.Should().NotBeNull();
        footer!.PlainText.Should().Contain("/unsubscribe?token=");
        footer.PlainText.Should().Contain("/workspace/settings/notifications");
        footer.Html.Should().Contain("/unsubscribe?token=");
        footer.Html.Should().Contain("/workspace/settings/notifications");

        // Extract the token from the unsubscribe URL and validate it via the token service.
        var token = ExtractToken(footer.Html);
        var result = tokenService.Validate(token);
        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(UserId);
        result.Scope.Should().Be("type");
        result.Value.Should().Be(NotificationType.MonthlyStatement.ToString());
    }

    [Fact]
    public void Build_For_Transactional_Type_Returns_Null()
    {
        var subject = CreateSubject();
        var notification = Notification.Create(
            UserId, NotificationType.PasswordReset, NotificationChannel.Email, "Reset", "Body");

        var footer = subject.Build(notification);

        footer.Should().BeNull();
    }

    [Fact]
    public void Build_For_Null_Recipient_Returns_Null_And_Generates_No_Token()
    {
        var tokenService = new UnsubscribeTokenService(new EphemeralDataProtectionProvider());
        var subject = CreateSubject(tokenService);
        var notification = NullRecipientNotification();

        var footer = subject.Build(notification);

        footer.Should().BeNull();
    }

    [Fact]
    public void Build_When_BaseUrl_Unset_Falls_Back_To_Localhost()
    {
        var subject = CreateSubject(configuration: new ConfigurationBuilder().Build());

        var footer = subject.Build(SuppressibleNotification());

        footer.Should().NotBeNull();
        footer!.PlainText.Should().Contain("http://localhost:3000/unsubscribe?token=");
        footer.PlainText.Should().Contain("http://localhost:3000/workspace/settings/notifications");
    }

    [Fact]
    public void Build_Html_Encodes_Interpolated_Values()
    {
        var subject = CreateSubject();
        var notification = SuppressibleNotification();

        var footer = subject.Build(notification);

        // HtmlEncode is applied to the interpolated URLs: no raw '&' may leak into the html variant.
        footer!.Html.Should().NotContain("&");
        footer.Html.Should().Contain("<a href=\"");
    }

    private static string ExtractToken(string html)
    {
        const string marker = "/unsubscribe?token=";
        var start = html.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = html.IndexOf('"', start);
        return html[start..end];
    }
}

public sealed class EmailRendererBaseTests
{
    private sealed class TestRenderer : EmailRendererBase
    {
        public bool Suppressible(NotificationType type) => IsSuppressible(type);
        public bool Footer(Notification n) => HasFooter(n);
        public (string, string) Merge(string p, string h, EmailFooter? f) => MergeFooter(p, h, f);
    }

    private readonly TestRenderer _renderer = new();

    [Fact]
    public void IsSuppressible_Is_False_For_Transactional_Types()
    {
        _renderer.Suppressible(NotificationType.EmailVerification).Should().BeFalse();
        _renderer.Suppressible(NotificationType.PasswordReset).Should().BeFalse();
        _renderer.Suppressible(NotificationType.MagicLink).Should().BeFalse();
        _renderer.Suppressible(NotificationType.TenantInvite).Should().BeFalse();
    }

    [Fact]
    public void IsSuppressible_Is_True_For_Non_Transactional_Types()
    {
        _renderer.Suppressible(NotificationType.MonthlyStatement).Should().BeTrue();
        _renderer.Suppressible(NotificationType.Marketing).Should().BeTrue();
    }

    [Fact]
    public void HasFooter_Is_False_For_Null_Recipient()
    {
        var notification = Notification.Create(
            null, NotificationType.MonthlyStatement, NotificationChannel.Email, "S", "B");
        _renderer.Footer(notification).Should().BeFalse();
    }

    [Fact]
    public void MergeFooter_Appends_Footer_When_Present()
    {
        var (plain, html) = _renderer.Merge("body", "<p>body</p>", new EmailFooter("foot", "<p>foot</p>"));

        plain.Should().Be("body\n\nfoot");
        html.Should().Be("<p>body</p><p>foot</p>");
    }

    [Fact]
    public void MergeFooter_Returns_Body_Unchanged_When_Footer_Null()
    {
        var (plain, html) = _renderer.Merge("body", "<p>body</p>", null);

        plain.Should().Be("body");
        html.Should().Be("<p>body</p>");
    }
}
