using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests;

/// <summary>
///     Issue #409 remainder: PDF export of the fraud statistics report. The composer
///     emits a structurally valid PDF 1.4 document (header, xref, trailer) whose text
///     layer carries the report sections, paginates long trend series, and sanitizes
///     non-ASCII input — following the monthly-statement PDF writer precedent.
/// </summary>
public class FraudStatisticsPdfComposerTests
{
    [Fact]
    public void Compose_Should_Emit_A_Valid_Pdf_Skeleton()
    {
        var pdf = FraudStatisticsPdfComposer.Compose(Report());

        var text = Encoding.ASCII.GetString(pdf);
        text.Should().StartWith("%PDF-1.4");
        text.Should().EndWith("%%EOF");
        text.Should().Contain("xref");
        text.Should().Contain("trailer");
        text.Should().Contain("/Type /Catalog");
        text.Should().Contain("/Type /Page");
        text.Should().Contain("/BaseFont /Helvetica");
    }

    [Fact]
    public void Compose_Should_Carry_The_Report_Sections_In_The_Text_Layer()
    {
        var pdf = FraudStatisticsPdfComposer.Compose(Report());

        var text = Encoding.ASCII.GetString(pdf);
        text.Should().Contain("GameGuild fraud statistics report");
        text.Should().Contain("Summary");
        text.Should().Contain("Held for review: 3");
        text.Should().Contain("Review queue");
        text.Should().Contain("Top signals");
        text.Should().Contain("Velocity.PaymentFrequency: 12");
        text.Should().Contain("By billing country");
        text.Should().Contain("By payment method");
        text.Should().Contain("By subscription plan");
        text.Should().Contain("Monthly trend", "the header carries the enum spelling of the granularity");
        text.Should().Contain("Chargeback rate: 1.50%");
    }

    [Fact]
    public void Compose_Should_Paginate_Long_Trend_Series()
    {
        var report = Report() with { };
        var trend = Enumerable.Range(0, 200)
            .Select(index => new FraudStatisticsTrendPoint
            {
                Day = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(index),
                Created = index,
                ReviewHeld = 0,
                Blocked = 0
            })
            .ToList();
        report = report with { Trend = trend };

        var pdf = FraudStatisticsPdfComposer.Compose(report);

        var text = Encoding.ASCII.GetString(pdf);
        var pageCount = CountPages(text);
        pageCount.Should().BeGreaterThan(1, "200 trend points exceed one page");
    }

    [Fact]
    public void Compose_Should_Sanitize_Non_Ascii_Input_In_The_Text_Layer()
    {
        var report = Report(country: "Ünïcode Cöuntry €");

        var pdf = FraudStatisticsPdfComposer.Compose(report);

        var text = Encoding.ASCII.GetString(pdf);
        text.Should().NotContain("Ünïcode", "non-ASCII characters are folded out of the Helvetica text layer");
        text.Should().Contain("C");
    }

    [Fact]
    public void Compose_Should_Escape_Pdf_Literals()
    {
        var report = Report(country: "A(B)\\C");

        var pdf = FraudStatisticsPdfComposer.Compose(report);

        var text = Encoding.ASCII.GetString(pdf);
        text.Should().Contain("A\\(B\\)\\\\C");
    }

    private static int CountPages(string text) =>
        Regex.Matches(text, "/Type /Page[^s]").Count;

    private static FraudStatisticsReport Report(string country = "US") =>
        new()
        {
            GeneratedAtUtc = new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc),
            FromUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc),
            Totals = new FraudStatisticsTotals
            {
                PaymentsCreated = 120,
                Assessed = 100,
                Allowed = 90,
                ReviewHeld = 3,
                Blocked = 7,
                Succeeded = 80,
                Disputed = 1
            },
            Rates = new FraudStatisticsRates
            {
                FlaggedPercent = 10.0,
                BlockedPercent = 7.0,
                ChargebackPercent = 1.5,
                FalsePositivePercent = 33.3
            },
            Reviews = new FraudStatisticsReviews
            {
                PendingBacklog = 2,
                ResolvedInWindow = 4,
                ApprovedInWindow = 3,
                DeclinedInWindow = 1,
                AverageReviewHours = 5.5
            },
            TopSignals =
            [
                new FraudStatisticsSignal { SignalType = "Velocity.PaymentFrequency", Count = 12 },
                new FraudStatisticsSignal { SignalType = "Device.CrossAccountUsage", Count = 4 }
            ],
            ByCountry =
            [
                new FraudStatisticsCountryRow { Country = country, Created = 100, ReviewHeld = 3, Blocked = 7 }
            ],
            ByPaymentMethod =
            [
                new FraudStatisticsMethodRow { Provider = "stripe", Created = 90, ReviewHeld = 2, Blocked = 5 }
            ],
            ByPlan =
            [
                new FraudStatisticsPlanRow { PlanId = Guid.NewGuid(), PlanName = "Pro", Created = 60, ReviewHeld = 1, Blocked = 2 }
            ],
            TrendGranularity = FraudTrendGranularity.Monthly,
            Trend =
            [
                new FraudStatisticsTrendPoint
                {
                    Day = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    Created = 120,
                    ReviewHeld = 3,
                    Blocked = 7
                }
            ]
        };
}
