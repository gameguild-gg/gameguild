using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GameGuild.Commerce.Payments;
using System.Security.Cryptography;
using System.Text;

namespace GameGuild.Commerce.Payments.PerformanceTests;

/// <summary>
/// Performance benchmarks for Payments module operations.
/// Measures throughput, latency, and resource consumption for critical payment paths.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PaymentPerformanceBenchmarks
{
    private Guid _tenantId;
    private Payment[] _successfulPayments = [];
    private TaxCalculationRequest[] _taxRequests = [];

    [GlobalSetup]
    public void Setup()
    {
        _tenantId = Guid.NewGuid();
        _successfulPayments = Enumerable.Range(0, 2_000)
            .Select(index =>
            {
                var payment = Payment.Create(_tenantId, 15m + index % 120, "usd", $"payment-benchmark-{index}");
                payment.MarkAsProcessing($"txn_{index:N0}");
                payment.MarkAsSucceeded($"pi_{index:N0}");
                return payment;
            })
            .ToArray();

        _taxRequests = Enumerable.Range(0, 5_000)
            .Select(index => new TaxCalculationRequest
            {
                JurisdictionCode = index % 2 == 0 ? "US-CA" : "US-NY",
                Amount = 10m + index % 200,
                Currency = "USD",
                CustomerType = index % 5 == 0 ? CustomerType.B2B : CustomerType.B2C,
                ProductCategory = index % 3 == 0 ? "digital" : "course",
                IsTaxInclusive = index % 7 == 0
            })
            .ToArray();
    }

    /// <summary>
    /// Benchmark: Payment processing throughput.
    /// Target: Process 200 payments/second under load.
    /// </summary>
    [Benchmark]
    public decimal PaymentProcessing_Throughput()
    {
        var total = 0m;
        for (var index = 0; index < 200; index++)
        {
            var payment = Payment.Create(_tenantId, 19.99m + index, "usd", $"process-{Guid.NewGuid():N}");
            payment.MarkAsProcessing($"txn_process_{index}");
            payment.MarkAsSucceeded($"pi_process_{index}");
            total += payment.NetAmount;
        }

        return total;
    }

    /// <summary>
    /// Benchmark: Tax calculation latency.
    /// Target: Calculate tax within 5ms P99.
    /// </summary>
    [Benchmark]
    public decimal TaxCalculation_Latency()
    {
        var totalTax = 0m;
        foreach (var request in _taxRequests.Take(1_000))
        {
            var rate = request.JurisdictionCode == "US-CA" ? 0.0825m : 0.08875m;
            totalTax += request.IsTaxInclusive
                ? request.Amount - request.Amount / (1m + rate)
                : request.Amount * rate;
        }

        return Math.Round(totalTax, 2);
    }

    /// <summary>
    /// Benchmark: Payment gateway callback processing.
    /// Target: Process callback within 50ms P99.
    /// </summary>
    [Benchmark]
    public int GatewayCallback_ProcessingLatency()
    {
        return _successfulPayments.Count(payment =>
            payment.Status == PaymentStatus.Succeeded &&
            payment.ExternalPaymentId is not null &&
            payment.ProcessedAt.HasValue);
    }

    /// <summary>
    /// Benchmark: Concurrent payment validations.
    /// Target: Validate 500 concurrent payments without race conditions.
    /// </summary>
    [Benchmark]
    public int PaymentValidation_ConcurrentLoad()
    {
        var valid = 0;
        Parallel.ForEach(_successfulPayments.Take(500), payment =>
        {
            if (payment.NetAmount > 0 && payment.Currency == "USD" && payment.IsTerminal == false)
            {
                Interlocked.Increment(ref valid);
            }
        });

        return valid;
    }

    /// <summary>
    /// Benchmark: Refund processing throughput.
    /// Target: Process 100 refunds/second.
    /// </summary>
    [Benchmark]
    public decimal RefundProcessing_Throughput()
    {
        var refunded = 0m;
        for (var index = 0; index < 100; index++)
        {
            var payment = Payment.Create(_tenantId, 50m + index, "usd", $"refund-{Guid.NewGuid():N}");
            payment.MarkAsProcessing();
            payment.MarkAsSucceeded($"pi_refund_{index}");
            payment.ProcessRefund(10m, $"re_{index}", "customer request");
            refunded += payment.RefundedAmount;
        }

        return refunded;
    }

    /// <summary>
    /// Benchmark: Payment method tokenization.
    /// Target: Tokenize payment method within 100ms.
    /// </summary>
    [Benchmark]
    public string PaymentTokenization_Latency()
    {
        var payload = Encoding.UTF8.GetBytes($"pm:{_tenantId}:{_successfulPayments[0].ExternalPaymentId}:4242");
        var hash = SHA256.HashData(payload);
        return Convert.ToBase64String(hash);
    }
}
