using GameGuild.Notifications.UnitTests.Infrastructure;

namespace GameGuild.Notifications.UnitTests.Entities;

public class EmailSuppressionTests
{
    [Fact]
    public void Create_Should_Initialize_All_Fields_And_Normalize_Address()
    {
        var sourceEventId = Guid.NewGuid();

        var suppression = EmailSuppression.Create(
            "  Bounced@Example.COM ",
            EmailSuppressionReason.HardBounce,
            "Permanent",
            sourceEventId);

        suppression.Id.Should().NotBe(Guid.Empty);
        suppression.EmailAddress.Should().Be("bounced@example.com");
        suppression.Reason.Should().Be(EmailSuppressionReason.HardBounce);
        suppression.BounceType.Should().Be("Permanent");
        suppression.SourceEventId.Should().Be(sourceEventId);
        suppression.SuppressedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
        suppression.ReleasedAt.Should().BeNull();
        suppression.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Release_Should_Set_ReleasedAt_And_Deactivate()
    {
        var suppression = EmailSuppression.Create(
            "complaint@example.com",
            EmailSuppressionReason.Complaint);

        suppression.Release();

        suppression.ReleasedAt.Should().NotBeNull();
        suppression.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Suppression_Should_Roundtrip_Through_The_Context()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var context = CreateContext(databaseName);
        var suppression = EmailSuppression.Create(
            "RoundTrip@Example.com",
            EmailSuppressionReason.Complaint);

        context.EmailSuppressions.Add(suppression);
        await context.SaveChangesAsync();

        using var readContext = CreateContext(databaseName);
        var stored = await readContext.EmailSuppressions.SingleAsync(s => s.EmailAddress == "roundtrip@example.com");

        stored.Reason.Should().Be(EmailSuppressionReason.Complaint);
        stored.IsActive.Should().BeTrue();
        stored.Id.Should().Be(suppression.Id);
    }

    private static NotificationsTestDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new NotificationsTestDbContext(options);
    }
}
