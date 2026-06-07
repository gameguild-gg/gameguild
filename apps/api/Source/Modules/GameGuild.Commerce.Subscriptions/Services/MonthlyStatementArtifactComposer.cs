using System.Globalization;
using System.Text;

namespace GameGuild.Commerce.Subscriptions;

public interface IMonthlyStatementDataProvider
{
    Task<MonthlyStatementBuildContext> BuildAsync(
        Guid tenantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
}

public sealed record MonthlyStatementBuildContext(
    MonthlyStatementSourceData SourceData,
    MonthlyStatementDocumentOptions DocumentOptions);

public sealed record MonthlyStatementSourceData(
    Guid TenantId,
    DateTime GeneratedAtUtc,
    DateOnly FromDate,
    DateOnly ToDate,
    int LedgerCount,
    int RootLedgerCount,
    int EntryCount,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal NetCashFlow,
    decimal ClosingBalance,
    IReadOnlyList<StatementCategorySummary> Categories,
    IReadOnlyList<StatementPeriodSummary> Periods,
    IReadOnlyList<StatementTransactionSummary> Transactions,
    IReadOnlyList<StatementOwnerSummary> OwnerStatements,
    IReadOnlyList<StatementRenterSummary> RenterPayments,
    StatementMaintenanceSummary? MaintenanceReport);

public sealed record MonthlyStatementDocumentOptions(
    string FileStem,
    string ReportTitle,
    MonthlyStatementDocumentProfile DocumentProfile);

public enum MonthlyStatementDocumentProfile
{
    Compact,
    Detailed,
}

public static class MonthlyStatementArtifactComposer
{
    private static readonly CultureInfo EnUsCulture = CultureInfo.GetCultureInfo("en-US");

    public static MonthlyStatementArtifacts Compose(MonthlyStatementBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var source = context.SourceData;
        var options = context.DocumentOptions;

        var report = new MonthlyStatementReport(
            source.TenantId,
            source.GeneratedAtUtc,
            source.FromDate,
            source.ToDate,
            source.LedgerCount,
            source.RootLedgerCount,
            source.EntryCount,
            source.TotalDebit,
            source.TotalCredit,
            source.NetCashFlow,
            source.ClosingBalance,
            source.Categories,
            source.Periods,
            source.Transactions,
            source.OwnerStatements,
            source.RenterPayments,
            source.MaintenanceReport);

        var attachments = new List<MonthlyStatementEmailAttachment>
        {
            new($"{options.FileStem}.csv", "text/csv", Encoding.UTF8.GetBytes(BuildCsv(report, options.DocumentProfile))),
            new($"{options.FileStem}.pdf", "application/pdf", BuildPdf(report, options)),
        };

        return new MonthlyStatementArtifacts(report, attachments);
    }

    private static string BuildCsv(MonthlyStatementReport report, MonthlyStatementDocumentProfile profile)
        => profile switch
        {
            MonthlyStatementDocumentProfile.Compact => BuildCompactCsv(report),
            MonthlyStatementDocumentProfile.Detailed => BuildDetailedCsv(report),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
        };

    private static byte[] BuildPdf(MonthlyStatementReport report, MonthlyStatementDocumentOptions options)
        => options.DocumentProfile switch
        {
            MonthlyStatementDocumentProfile.Compact => BuildCompactPdf(report, options.ReportTitle),
            MonthlyStatementDocumentProfile.Detailed => BuildDetailedPdf(report, options.ReportTitle),
            _ => throw new ArgumentOutOfRangeException(nameof(options.DocumentProfile), options.DocumentProfile, null)
        };

    private static string BuildCompactCsv(MonthlyStatementReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Metric,Value");
        builder.AppendLine($"TenantId,{report.TenantId}");
        builder.AppendLine($"GeneratedAtUtc,{report.GeneratedAtUtc:O}");
        builder.AppendLine($"FromDate,{report.FromDate:yyyy-MM-dd}");
        builder.AppendLine($"ToDate,{report.ToDate:yyyy-MM-dd}");
        builder.AppendLine($"EntryCount,{report.EntryCount}");
        builder.AppendLine($"TotalCredit,{report.TotalCredit.ToString("F2", EnUsCulture)}");
        builder.AppendLine($"TotalDebit,{report.TotalDebit.ToString("F2", EnUsCulture)}");
        builder.AppendLine($"NetCashFlow,{report.NetCashFlow.ToString("F2", EnUsCulture)}");
        builder.AppendLine($"ClosingBalance,{report.ClosingBalance.ToString("F2", EnUsCulture)}");
        builder.AppendLine();
        builder.AppendLine("Date,Provider,Status,Description,Amount");

        foreach (var transaction in report.Transactions)
        {
            builder.AppendLine(string.Join(",",
                EscapeCompactCsv(transaction.TransactionDate),
                EscapeCompactCsv(transaction.LedgerCode),
                EscapeCompactCsv(transaction.Status),
                EscapeCompactCsv(transaction.Description),
                transaction.Amount.ToString("F2", EnUsCulture)));
        }

        return builder.ToString();
    }

    private static string BuildDetailedCsv(MonthlyStatementReport report)
    {
        var rows = new List<object?[]>
        {
            new object?[] { "metric", "value" },
            new object?[] { "generated_at_utc", report.GeneratedAtUtc.ToString("O", CultureInfo.InvariantCulture) },
            new object?[] { "from_date", report.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
            new object?[] { "to_date", report.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
            new object?[] { "ledger_count", report.LedgerCount },
            new object?[] { "root_ledger_count", report.RootLedgerCount },
            new object?[] { "entry_count", report.EntryCount },
            new object?[] { "total_debit", report.TotalDebit },
            new object?[] { "total_credit", report.TotalCredit },
            new object?[] { "net_cash_flow", report.NetCashFlow },
            new object?[] { "closing_balance", report.ClosingBalance },
            Array.Empty<object?>(),
            new object?[] { "category", "total_debit", "total_credit", "net_amount", "entry_count", "percentage_of_total" },
        };

        rows.AddRange(report.Categories.Select(category => new object?[]
        {
            category.Category,
            category.TotalDebit,
            category.TotalCredit,
            category.NetAmount,
            category.EntryCount,
            category.PercentageOfTotal,
        }));

        rows.Add(Array.Empty<object?>());
        rows.Add(new object?[] { "period_label", "period_start", "period_end", "total_debit", "total_credit", "net_change", "running_balance", "entry_count" });
        rows.AddRange(report.Periods.Select(period => new object?[]
        {
            period.PeriodLabel,
            period.PeriodStart,
            period.PeriodEnd,
            period.TotalDebit,
            period.TotalCredit,
            period.NetChange,
            period.RunningBalance,
            period.EntryCount,
        }));

        rows.Add(Array.Empty<object?>());
        rows.Add(new object?[] { "transaction_id", "transaction_date", "ledger_code", "type", "category", "description", "amount", "status", "counterparty" });
        rows.AddRange(report.Transactions.Select(transaction => new object?[]
        {
            transaction.Id,
            transaction.TransactionDate,
            transaction.LedgerCode,
            transaction.Type,
            transaction.Category,
            transaction.Description,
            transaction.Amount,
            transaction.Status,
            transaction.CounterpartyName ?? string.Empty,
        }));

        rows.Add(Array.Empty<object?>());
        rows.Add(new object?[] { "owner_id", "owner_name", "email", "property_count", "estimated_monthly_gross_usd", "estimated_monthly_expenses_usd", "approved_maintenance_usd", "estimated_monthly_net_usd" });
        rows.AddRange(report.OwnerStatements.Select(owner => new object?[]
        {
            owner.OwnerId,
            owner.OwnerName,
            owner.Email,
            owner.PropertyCount,
            owner.EstimatedMonthlyGrossUsd,
            owner.EstimatedMonthlyExpensesUsd,
            owner.ApprovedMaintenanceUsd,
            owner.EstimatedMonthlyNetUsd,
        }));

        rows.Add(Array.Empty<object?>());
        rows.Add(new object?[] { "renter_id", "renter_name", "email", "property_count", "payment_count", "total_billed_usd", "total_paid_usd", "overdue_count", "current_due_usd" });
        rows.AddRange(report.RenterPayments.Select(renter => new object?[]
        {
            renter.RenterId,
            renter.RenterName,
            renter.Email,
            renter.PropertyCount,
            renter.PaymentCount,
            renter.TotalBilledUsd,
            renter.TotalPaidUsd,
            renter.OverdueCount,
            renter.CurrentDueUsd,
        }));

        if (report.MaintenanceReport is not null)
        {
            rows.Add(Array.Empty<object?>());
            rows.Add(new object?[] { "maintenance_metric", "value" });
            rows.Add(new object?[] { "maintenance_generated_at_utc", report.MaintenanceReport.GeneratedAtUtc.ToString("O", CultureInfo.InvariantCulture) });
            rows.Add(new object?[] { "maintenance_ticket_count", report.MaintenanceReport.TicketCount });
            rows.Add(new object?[] { "maintenance_open_ticket_count", report.MaintenanceReport.OpenTicketCount });
            rows.Add(new object?[] { "maintenance_quote_count", report.MaintenanceReport.QuoteCount });
            rows.Add(new object?[] { "maintenance_pending_quote_count", report.MaintenanceReport.PendingQuoteCount });
            rows.Add(new object?[] { "maintenance_overdue_quote_count", report.MaintenanceReport.OverdueQuoteCount });
        }

        return string.Join(
            "\n",
            rows.Select(row => string.Join(",", row.Select(EscapeDetailedCsvValue))));
    }

    private static byte[] BuildCompactPdf(MonthlyStatementReport report, string reportTitle)
    {
        var lines = new List<string>
        {
            reportTitle,
            $"Tenant: {report.TenantId}",
            $"Period: {report.FromDate:yyyy-MM-dd} to {report.ToDate:yyyy-MM-dd}",
            $"Generated: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC",
            $"Entries: {report.EntryCount}",
            $"Total credit: {report.TotalCredit.ToString("F2", EnUsCulture)}",
            $"Total debit: {report.TotalDebit.ToString("F2", EnUsCulture)}",
            $"Net cash flow: {report.NetCashFlow.ToString("F2", EnUsCulture)}",
            $"Closing balance: {report.ClosingBalance.ToString("F2", EnUsCulture)}",
        };

        foreach (var category in report.Categories.Take(6))
        {
            lines.Add($"Category {category.Category}: credit {category.TotalCredit.ToString("F2", EnUsCulture)}, debit {category.TotalDebit.ToString("F2", EnUsCulture)}");
        }

        foreach (var transaction in report.Transactions.Take(8))
        {
            lines.Add($"{transaction.TransactionDate} | {transaction.LedgerCode} | {transaction.Status} | {transaction.Amount.ToString("F2", EnUsCulture)}");
        }

        return BuildSimplePdf(lines);
    }

    private static byte[] BuildDetailedPdf(MonthlyStatementReport report, string reportTitle)
    {
        var lines = BuildDetailedPdfLines(report, reportTitle)
            .SelectMany(line => WrapPdfLine(line, 94))
            .Select(SanitizePdfText)
            .ToList();

        var pages = lines.Chunk(46).ToList();

        var objects = new Dictionary<int, string>();
        objects[1] = "<< /Type /Catalog /Pages 2 0 R >>";
        objects[2] = $"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(0, pages.Count).Select(index => $"{4 + (index * 2)} 0 R"))}] /Count {pages.Count} >>";
        objects[3] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";

        for (var index = 0; index < pages.Count; index++)
        {
            var pageObjectNumber = 4 + (index * 2);
            var contentObjectNumber = pageObjectNumber + 1;
            var contentStream = BuildDetailedPdfContentStream(pages[index]);
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

    private static IEnumerable<string> BuildDetailedPdfLines(MonthlyStatementReport report, string reportTitle)
    {
        yield return reportTitle;
        yield return $"Generated: {report.GeneratedAtUtc:O}";
        yield return $"Period: {report.FromDate:yyyy-MM-dd} to {report.ToDate:yyyy-MM-dd}";
        yield return string.Empty;
        yield return "Summary";
        yield return $"Ledger count: {report.LedgerCount}";
        yield return $"Entry count: {report.EntryCount}";
        yield return $"Total debit: {FormatCurrency(report.TotalDebit)}";
        yield return $"Total credit: {FormatCurrency(report.TotalCredit)}";
        yield return $"Net cash flow: {FormatCurrency(report.NetCashFlow)}";
        yield return $"Closing balance: {FormatCurrency(report.ClosingBalance)}";
        yield return string.Empty;
        yield return "Category Rollup";
        foreach (var category in report.Categories.Take(10))
        {
            yield return $"{category.Category}: debit {FormatCurrency(category.TotalDebit)}, credit {FormatCurrency(category.TotalCredit)}, net {FormatCurrency(category.NetAmount)} ({category.EntryCount} entries)";
        }

        yield return string.Empty;
        yield return "Period Rollup";
        foreach (var period in report.Periods.TakeLast(12))
        {
            yield return $"{period.PeriodLabel}: debit {FormatCurrency(period.TotalDebit)}, credit {FormatCurrency(period.TotalCredit)}, net {FormatCurrency(period.NetChange)}, running {FormatCurrency(period.RunningBalance)}";
        }

        yield return string.Empty;
        yield return "Owner Statements";
        if (report.OwnerStatements.Count == 0)
        {
            yield return "No owner statements available in the selected period.";
        }
        else
        {
            foreach (var owner in report.OwnerStatements.Take(10))
            {
                yield return $"{owner.OwnerName}: net {FormatCurrency(owner.EstimatedMonthlyNetUsd)}, gross {FormatCurrency(owner.EstimatedMonthlyGrossUsd)}, maintenance {FormatCurrency(owner.ApprovedMaintenanceUsd)}, properties {owner.PropertyCount}";
            }
        }

        yield return string.Empty;
        yield return "Renter Payments";
        if (report.RenterPayments.Count == 0)
        {
            yield return "No renter payment history available in the selected period.";
        }
        else
        {
            foreach (var renter in report.RenterPayments.Take(10))
            {
                yield return $"{renter.RenterName}: billed {FormatCurrency(renter.TotalBilledUsd)}, paid {FormatCurrency(renter.TotalPaidUsd)}, due {FormatCurrency(renter.CurrentDueUsd)}, overdue {renter.OverdueCount}";
            }
        }

        yield return string.Empty;
        yield return "Recent Transactions";
        foreach (var transaction in report.Transactions.Take(10))
        {
            yield return $"{transaction.TransactionDate} {transaction.LedgerCode} {transaction.Type} {transaction.Category} {FormatCurrency(transaction.Amount)} {transaction.Description}";
        }

        if (report.MaintenanceReport is not null)
        {
            yield return string.Empty;
            yield return "Maintenance Snapshot";
            yield return $"Open tickets: {report.MaintenanceReport.OpenTicketCount}";
            yield return $"Quotes: {report.MaintenanceReport.QuoteCount}";
            yield return $"Pending quotes: {report.MaintenanceReport.PendingQuoteCount}";
            yield return $"Overdue quotes: {report.MaintenanceReport.OverdueQuoteCount}";
        }
    }

    private static IEnumerable<string> WrapPdfLine(string line, int maxLength)
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

    private static string BuildDetailedPdfContentStream(IEnumerable<string> lines)
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
                .Append(EscapeDetailedPdfLiteral(line))
                .Append(") Tj\n");

            firstLine = false;
        }

        builder.Append("ET");
        return builder.ToString();
    }

    private static string SanitizePdfText(string value)
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

    private static string EscapeCompactCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string EscapeDetailedCsvValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var stringValue = value switch
        {
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

        return stringValue.IndexOfAny([',', '"', '\n']) >= 0
            ? $"\"{stringValue.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : stringValue;
    }

    private static byte[] BuildSimplePdf(IReadOnlyList<string> lines)
    {
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("BT");
        contentBuilder.AppendLine("/F1 11 Tf");
        contentBuilder.AppendLine("50 780 Td");
        contentBuilder.AppendLine("14 TL");

        foreach (var line in lines)
        {
            contentBuilder.AppendLine($"({EscapeSimplePdf(line)}) Tj");
            contentBuilder.AppendLine("T*");
        }

        contentBuilder.AppendLine("ET");

        var content = contentBuilder.ToString();
        var objects = new[]
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj\n",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n",
            $"5 0 obj << /Length {Encoding.ASCII.GetByteCount(content)} >> stream\n{content}endstream\nendobj\n"
        };

        var documentBuilder = new StringBuilder();
        documentBuilder.Append("%PDF-1.4\n");

        var offsets = new List<int>();
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(documentBuilder.ToString()));
            documentBuilder.Append(obj);
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(documentBuilder.ToString());
        documentBuilder.Append($"xref\n0 {objects.Length + 1}\n");
        documentBuilder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            documentBuilder.Append($"{offset:D10} 00000 n \n");
        }

        documentBuilder.Append($"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\n");
        documentBuilder.Append($"startxref\n{xrefOffset}\n%%EOF");

        return Encoding.ASCII.GetBytes(documentBuilder.ToString());
    }

    private static string EscapeSimplePdf(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    private static string EscapeDetailedPdfLiteral(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    private static string FormatCurrency(decimal value) => value.ToString("C2", EnUsCulture);
}
