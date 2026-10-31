using System.Globalization;
using System.Text;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Serializes a <see cref="FraudStatisticsReport"/> to PDF (issue #409: export fraud
///     reports — CSV and PDF). Follows the repository's established minimal PDF writer
///     precedent (MonthlyStatementArtifactComposer): a hand-rolled PDF 1.4 document with
///     a single embedded Helvetica font, ASCII-sanitized text, wrapped lines and fixed
///     pagination — no heavyweight PDF dependency. Sections mirror the CSV export:
///     summary, review queue, top signals, per-country, per-provider, per-plan and the
///     trend series at the report granularity.
/// </summary>
public static class FraudStatisticsPdfComposer
{
    private const int LineWidth = 94;
    private const int LinesPerPage = 46;
    private static readonly CultureInfo EnUsCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Composes the PDF bytes of the report (ASCII, PDF 1.4, multi-page).</summary>
    public static byte[] Compose(FraudStatisticsReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var lines = BuildLines(report)
            .SelectMany(line => WrapLine(line, LineWidth))
            .Select(SanitizeText)
            .ToList();

        var pages = lines.Chunk(LinesPerPage).ToList();
        if (pages.Count == 0)
        {
            pages.Add([]);
        }

        var objects = new Dictionary<int, string>();
        objects[1] = "<< /Type /Catalog /Pages 2 0 R >>";
        objects[2] = $"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(0, pages.Count).Select(index => $"{4 + (index * 2)} 0 R"))}] /Count {pages.Count} >>";
        objects[3] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";

        for (var index = 0; index < pages.Count; index++)
        {
            var pageObjectNumber = 4 + (index * 2);
            var contentObjectNumber = pageObjectNumber + 1;
            var contentStream = BuildContentStream(pages[index]);
            var contentLength = Encoding.ASCII.GetByteCount(contentStream);

            objects[contentObjectNumber] = $"<< /Length {contentLength} >>\nstream\n{contentStream}\nendstream";
            objects[pageObjectNumber] = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectNumber} 0 R >>";
        }

        var pdfBuilder = new StringBuilder();
        pdfBuilder.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };
        var objectCount = objects.Count;
        for (var objectNumber = 1; objectNumber <= objectCount; objectNumber++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdfBuilder.ToString()));
            pdfBuilder
                .Append(objectNumber)
                .Append(" 0 obj\n")
                .Append(objects[objectNumber])
                .Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(pdfBuilder.ToString());
        pdfBuilder
            .Append("xref\n0 ")
            .Append(objectCount + 1)
            .Append("\n")
            .Append("0000000000 65535 f \n");

        foreach (var offset in offsets.Skip(1))
        {
            pdfBuilder
                .Append(offset.ToString("0000000000", CultureInfo.InvariantCulture))
                .Append(" 00000 n \n");
        }

        pdfBuilder
            .Append("trailer\n<< /Size ")
            .Append(objectCount + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset.ToString(CultureInfo.InvariantCulture))
            .Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(pdfBuilder.ToString());
    }

    private static IEnumerable<string> BuildLines(FraudStatisticsReport report)
    {
        yield return "GameGuild fraud statistics report";
        yield return $"Generated: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC";
        yield return $"Window: {report.FromUtc:yyyy-MM-dd} to {report.ToUtc:yyyy-MM-dd} (trend granularity: {report.TrendGranularity})";
        yield return string.Empty;
        yield return "Summary";
        yield return $"Payments created: {report.Totals.PaymentsCreated}";
        yield return $"Assessed: {report.Totals.Assessed}";
        yield return $"Allowed: {report.Totals.Allowed}";
        yield return $"Held for review: {report.Totals.ReviewHeld}";
        yield return $"Blocked: {report.Totals.Blocked}";
        yield return $"Succeeded: {report.Totals.Succeeded}";
        yield return $"Disputed (chargebacks): {report.Totals.Disputed}";
        yield return $"Flagged rate: {FormatRate(report.Rates.FlaggedPercent)}";
        yield return $"Blocked rate: {FormatRate(report.Rates.BlockedPercent)}";
        yield return $"Chargeback rate: {FormatRate(report.Rates.ChargebackPercent)}";
        yield return $"False-positive proxy: {FormatRate(report.Rates.FalsePositivePercent)}";
        yield return string.Empty;
        yield return "Review queue";
        yield return $"Pending backlog: {report.Reviews.PendingBacklog}";
        yield return $"Resolved in window: {report.Reviews.ResolvedInWindow} (approved {report.Reviews.ApprovedInWindow}, declined {report.Reviews.DeclinedInWindow})";
        yield return $"Average time to review: {(report.Reviews.AverageReviewHours.HasValue ? $"{report.Reviews.AverageReviewHours.Value:0.0}h" : "n/a")}";
        yield return string.Empty;
        yield return "Top signals";
        if (report.TopSignals.Count == 0)
        {
            yield return "No signals fired in the window.";
        }
        else
        {
            foreach (var signal in report.TopSignals)
            {
                yield return $"{signal.SignalType}: {signal.Count}";
            }
        }

        yield return string.Empty;
        yield return "By billing country";
        foreach (var country in report.ByCountry)
        {
            yield return $"{country.Country}: created {country.Created}, held {country.ReviewHeld}, blocked {country.Blocked}";
        }

        yield return string.Empty;
        yield return "By payment method";
        foreach (var method in report.ByPaymentMethod)
        {
            yield return $"{method.Provider}: created {method.Created}, held {method.ReviewHeld}, blocked {method.Blocked}";
        }

        yield return string.Empty;
        yield return "By subscription plan";
        foreach (var plan in report.ByPlan)
        {
            var planKey = plan.PlanId.HasValue ? $"{plan.PlanName} ({plan.PlanId})" : plan.PlanName;
            yield return $"{planKey}: created {plan.Created}, held {plan.ReviewHeld}, blocked {plan.Blocked}";
        }

        yield return string.Empty;
        yield return $"{report.TrendGranularity} trend";
        foreach (var point in report.Trend)
        {
            yield return $"{point.Day:yyyy-MM-dd}: created {point.Created}, held {point.ReviewHeld}, blocked {point.Blocked}";
        }
    }

    private static string FormatRate(double? rate) =>
        rate.HasValue ? $"{rate.Value:0.00}%" : "n/a";

    private static IEnumerable<string> WrapLine(string line, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            yield return string.Empty;
            yield break;
        }

        var remaining = line.Trim();
        while (remaining.Length > maxLength)
        {
            var splitIndex = remaining.LastIndexOf(' ', maxLength);
            if (splitIndex <= 0)
            {
                splitIndex = maxLength;
            }

            yield return remaining[..splitIndex].TrimEnd();
            remaining = remaining[splitIndex..].TrimStart();
        }

        if (remaining.Length > 0)
        {
            yield return remaining;
        }
    }

    private static string BuildContentStream(IEnumerable<string> lines)
    {
        var builder = new StringBuilder();
        builder.Append("BT\n/F1 10 Tf\n14 TL\n48 744 Td\n");
        var firstLine = true;
        foreach (var line in lines)
        {
            if (!firstLine)
            {
                builder.Append("T*\n");
            }

            builder.Append('(')
                .Append(EscapePdfLiteral(line))
                .Append(") Tj\n");

            firstLine = false;
        }

        builder.Append("ET");
        return builder.ToString();
    }

    private static string SanitizeText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (character is >= ' ' and <= '~')
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string EscapePdfLiteral(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
