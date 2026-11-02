using System.Text;
using FluentAssertions;
using GameGuild.Commerce.Payments;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Controllers;

/// <summary>
///     CSV round-trip contract of the fraud blacklist import/export (issue #409):
///     export composes RFC 4180-quoted CSV, import accepts it back (header optional),
///     rejects malformed rows with line numbers, and enforces bounded batch sizes.
/// </summary>
public class FraudBlacklistCsvTests
{
    private static FraudBlacklistEntryResult Entry(
        FraudBlacklistEntryType entryType = FraudBlacklistEntryType.IpAddress,
        string value = "203.0.113.*",
        string? reason = null,
        bool isWhitelist = false,
        DateTime? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        EntryType = entryType,
        Value = value,
        Reason = reason,
        IsWhitelist = isWhitelist,
        Source = FraudBlacklistSource.Manual,
        ExpiresAt = expiresAt,
        CreatedAt = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void Compose_Should_Write_The_Header_And_Every_Row()
    {
        var csv = Encoding.UTF8.GetString(FraudBlacklistCsv.Compose(
        [
            Entry(reason: "card testing"),
            Entry(FraudBlacklistEntryType.EmailDomain, "*.malicious.com", isWhitelist: true)
        ]));

        csv.Split('\n').First().Should().Be("entry_type,value,kind,reason,expires_at");
        csv.Should().Contain("IpAddress,203.0.113.*,blacklist,card testing,");
        csv.Should().Contain("EmailDomain,*.malicious.com,whitelist,,");
    }

    [Fact]
    public void Compose_Should_Quote_Fields_Containing_Commas_Quotes_Or_Newlines()
    {
        var csv = Encoding.UTF8.GetString(FraudBlacklistCsv.Compose(
        [
            Entry(reason: "seen in \"bursts\", multiple tenants")
        ]));

        csv.Should().Contain("\"seen in \"\"bursts\"\", multiple tenants\"");
    }

    [Fact]
    public void ParseImport_Should_Accept_An_Exported_File_Back()
    {
        var exported = Encoding.UTF8.GetString(FraudBlacklistCsv.Compose(
        [
            Entry(FraudBlacklistEntryType.Email, "fraud@example.com", reason: "confirmed fraud"),
            Entry(FraudBlacklistEntryType.DeviceFingerprint, "device_x", isWhitelist: false,
                expiresAt: new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        ]));

        var rows = FraudBlacklistCsv.ParseImport(exported);

        rows.Should().HaveCount(2);
        rows[0].EntryType.Should().Be(FraudBlacklistEntryType.Email);
        rows[0].Value.Should().Be("fraud@example.com");
        rows[0].Reason.Should().Be("confirmed fraud");
        rows[0].IsWhitelist.Should().BeFalse();
        rows[0].ExpiresAt.Should().BeNull();
        rows[1].EntryType.Should().Be(FraudBlacklistEntryType.DeviceFingerprint);
        rows[1].ExpiresAt.Should().Be(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ParseImport_Should_Skip_The_Header_Once_And_Ignore_Blank_Lines()
    {
        var rows = FraudBlacklistCsv.ParseImport(
            "ENTRY_TYPE,VALUE,KIND,REASON,EXPIRES_AT\r\n" +
            "\r\n" +
            "IpAddress,10.0.0.9,blacklist,,\r\n");

        rows.Should().ContainSingle();
        rows[0].Value.Should().Be("10.0.0.9");
        rows[0].LineNumber.Should().Be(3);
    }

    [Fact]
    public void ParseImport_Should_Unquote_Escaped_Fields()
    {
        var rows = FraudBlacklistCsv.ParseImport(
            "IpAddress,203.0.113.9,blacklist,\"quoted, \"\"reason\"\" row\",\r\n");

        rows.Should().ContainSingle();
        rows[0].Reason.Should().Be("quoted, \"reason\" row");
    }

    [Fact]
    public void ParseImport_Should_Reject_Unknown_Entry_Types_With_The_Line_Number()
    {
        var act = () => FraudBlacklistCsv.ParseImport("CarrierPigeon,coo,blacklist,,\r\n");

        act.Should().Throw<FraudBlacklistCsvFormatException>()
            .WithMessage("*line 1*unknown entry_type*CarrierPigeon*");
    }

    [Theory]
    [InlineData("IpAddress,203.0.113.9,graylist,,")]
    [InlineData("IpAddress,,blacklist,,")]
    [InlineData("IpAddress,203.0.113.9,blacklist,,not-a-date")]
    [InlineData("IpAddress,203.0.113.9")]
    public void ParseImport_Should_Reject_Malformed_Rows(string row)
    {
        var act = () => FraudBlacklistCsv.ParseImport(row + "\r\n");

        act.Should().Throw<FraudBlacklistCsvFormatException>().WithMessage("*line 1*");
    }

    [Fact]
    public void ParseImport_Should_Enforce_The_Row_Limit()
    {
        var row = "IpAddress,203.0.113.9,blacklist,,\r\n";
        var csv = string.Concat(Enumerable.Repeat(row, FraudBlacklistCsv.MaxImportRows + 1));

        var act = () => FraudBlacklistCsv.ParseImport(csv);

        act.Should().Throw<FraudBlacklistCsvFormatException>()
            .WithMessage($"*exceeds the {FraudBlacklistCsv.MaxImportRows}-row limit*");
    }

    [Fact]
    public void ParseImport_Should_Return_No_Rows_For_Empty_Input()
    {
        FraudBlacklistCsv.ParseImport(string.Empty).Should().BeEmpty();
    }
}
