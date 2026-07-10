using FluentAssertions;
using GameGuild.API.Email;
using GameGuild.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.API.UnitTests.Core.Email;

public sealed class EmailSenderTests
{
    [Fact]
    public async Task SendAsync_WhenDeliveryIsDisabled_ShouldSkipWithoutRequiredSenderSettings()
    {
        var sender = CreateSender(new EmailDeliveryOptions { Enabled = false });

        var act = () => sender.SendAsync(new EmailMessage(
            "member@example.com",
            "Invite",
            "Plain",
            "<p>Html</p>"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_WhenEnabledWithoutFromEmail_ShouldFailWithConfigurationError()
    {
        var sender = CreateSender(new EmailDeliveryOptions
        {
            Enabled = true,
            SmtpHost = "localhost",
        });

        var act = () => sender.SendAsync(new EmailMessage(
            "member@example.com",
            "Invite",
            "Plain",
            "<p>Html</p>"));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("EmailDelivery:FromEmail is required to send email.");
    }

    [Fact]
    public async Task SendAsync_WhenEnabledWithoutSmtpHost_ShouldFailWithConfigurationError()
    {
        var sender = CreateSender(new EmailDeliveryOptions
        {
            Enabled = true,
            FromEmail = "noreply@gameguild.gg",
        });

        var act = () => sender.SendAsync(new EmailMessage(
            "member@example.com",
            "Invite",
            "Plain",
            "<p>Html</p>"));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("EmailDelivery:SmtpHost is required when email delivery uses SMTP.");
    }

    private static EmailSender CreateSender(EmailDeliveryOptions options)
    {
        return new EmailSender(
            Options.Create(options),
            Mock.Of<ILogger<EmailSender>>());
    }
}
