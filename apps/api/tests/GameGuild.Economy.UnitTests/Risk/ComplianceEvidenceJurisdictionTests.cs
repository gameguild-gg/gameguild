using FluentAssertions;
using GameGuild.Economy.Risk;

namespace GameGuild.Economy.UnitTests.Risk;

public sealed class ComplianceEvidenceJurisdictionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_NormalizesAndCryptographicallyBindsJurisdiction()
    {
        var brazil = Create(" bra ");
        var germany = Create("DEU");

        brazil.JurisdictionCode.Should().Be("BRA");
        brazil.EvidenceHash.Should().NotBe(germany.EvidenceHash);
    }

    [Theory]
    [InlineData("BR")]
    [InlineData("12A")]
    public void Create_RejectsInvalidJurisdiction(string jurisdiction)
    {
        var act = () => Create(jurisdiction);

        act.Should().Throw<ArgumentException>();
    }

    private static ComplianceEvidenceEnvelope Create(string jurisdiction) =>
        ComplianceEvidenceEnvelope.Create(
            "sumsub",
            "sandbox",
            "event-1",
            Guid.Parse("92000000-0000-0000-0000-000000000001"),
            "subject-hash",
            1,
            ComplianceEvidenceResult.Approved,
            Now,
            Now.AddDays(30),
            7,
            new string('a', 64),
            true,
            "s3://evidence/event-1",
            Now,
            jurisdiction);
}
