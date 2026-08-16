using FluentAssertions;
using GameGuild.API.Integration;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Email;

namespace GameGuild.API.UnitTests.Core.Integration;

public sealed class MonthlyStatementMailSenderAdapterTests
{
    [Fact]
    public async Task SendAsync_PreservesMessageAndAttachments()
    {
        var emailSender = new CapturingEmailSender();
        var adapter = new MonthlyStatementMailSenderAdapter(emailSender);
        var content = new byte[] { 1, 2, 3 };
        using var cancellation = new CancellationTokenSource();
        var message = new MonthlyStatementEmailMessage(
            "member@example.com",
            "Monthly statement",
            "Plain text",
            "<p>HTML</p>",
            [new MonthlyStatementEmailAttachment("statement.pdf", "application/pdf", content)],
            "Member Name");

        await adapter.SendAsync(message, cancellation.Token);

        emailSender.CancellationToken.Should().Be(cancellation.Token);
        emailSender.Message.Should().NotBeNull();
        emailSender.Message!.ToEmail.Should().Be(message.ToEmail);
        emailSender.Message.ToName.Should().Be(message.ToName);
        emailSender.Message.Subject.Should().Be(message.Subject);
        emailSender.Message.PlainTextContent.Should().Be(message.PlainTextContent);
        emailSender.Message.HtmlContent.Should().Be(message.HtmlContent);
        emailSender.Message.Attachments.Should().ContainSingle();
        emailSender.Message.Attachments![0].FileName.Should().Be("statement.pdf");
        emailSender.Message.Attachments[0].ContentType.Should().Be("application/pdf");
        emailSender.Message.Attachments[0].Content.Should().Equal(content);
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public EmailMessage? Message { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Message = message;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
