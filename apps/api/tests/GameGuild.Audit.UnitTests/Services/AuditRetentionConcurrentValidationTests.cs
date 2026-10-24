using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using GameGuild.Compliance.Audit;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

public sealed class AuditRetentionConcurrentValidationTests
{
    [Theory]
    [InlineData(typeof(AuditStorageTierPrice), nameof(AuditStorageTierPrice.MonthlyCostPerGiB), "0", "100000")]
    [InlineData(typeof(AuditStorageTierPrice), nameof(AuditStorageTierPrice.RetrievalCostPerGiB), "0", "100000")]
    [InlineData(typeof(AuditStorageTierPrice), nameof(AuditStorageTierPrice.ExpectedReadLatencyMilliseconds), "0", "604800000")]
    [InlineData(typeof(ConfigureAuditRetentionRequest), nameof(ConfigureAuditRetentionRequest.StorageOverheadMultiplier), "1", "10")]
    [InlineData(typeof(ConfigureAuditRetentionRequest), nameof(ConfigureAuditRetentionRequest.MonthlyBudget), "0", "1000000000000000")]
    [InlineData(typeof(ConfigureAuditRetentionRequest), nameof(ConfigureAuditRetentionRequest.MaximumReadLatencyMilliseconds), "0", "604800000")]
    [InlineData(typeof(RunAuditRetentionSimulationRequest), nameof(RunAuditRetentionSimulationRequest.AnnualGrowthPercent), "-95", "300")]
    public void DecimalPropertiesRetainInclusiveRangeMetadataAndValidation(Type model, string property, string minimum, string maximum)
    {
        var range = Assert.IsType<ThreadSafeDecimalRangeAttribute>(model.GetProperty(property)!.GetCustomAttribute<RangeAttribute>());
        var lower = decimal.Parse(minimum, CultureInfo.InvariantCulture);
        var upper = decimal.Parse(maximum, CultureInfo.InvariantCulture);

        Assert.Equal(typeof(decimal), range.OperandType);
        Assert.True(range.IsValid(lower));
        Assert.True(range.IsValid(upper));
        Assert.False(range.IsValid(lower - 0.01m));
        Assert.False(range.IsValid(upper + 0.01m));
        Assert.True(range.IsValid(null));
        Assert.Contains(property, range.FormatErrorMessage(property));
    }

    [Fact]
    public void FreshSharedDecimalValidatorsSupportConcurrentValidationAndErrorFormatting()
    {
        for (var iteration = 0; iteration < 32; iteration++)
        {
            var range = new ThreadSafeDecimalRangeAttribute("-95", "300");
            Parallel.For(0, 64, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
            {
                if (index % 2 == 0)
                {
                    Assert.Contains("AnnualGrowthPercent", range.FormatErrorMessage("AnnualGrowthPercent"));
                }
                else
                {
                    Assert.True(range.IsValid(-95m));
                    Assert.True(range.IsValid(300m));
                    Assert.False(range.IsValid(300.01m));
                }
            });
        }
    }

    [Fact]
    public void CatalogStartupAndRequestValidationCanShareCachedAttributes()
    {
        Parallel.For(0, 32, new ParallelOptions { MaxDegreeOfParallelism = 8 }, _ =>
        {
            AuditRetentionPolicyTemplates.ValidateCatalog();
            var request = AuditRetentionPolicyTemplates.Baseline.ToConfigurationRequest();
            AuditRetentionInputValidation.Validate(request);

            var invalid = request with
            {
                StorageOverheadMultiplier = 0.99m,
                TierPrices = request.TierPrices.Select(price => price with { MonthlyCostPerGiB = -0.01m }).ToList()
            };
            var error = Assert.Throws<AuditRetentionValidationException>(() => AuditRetentionInputValidation.Validate(invalid));
            Assert.Contains(nameof(ConfigureAuditRetentionRequest.StorageOverheadMultiplier), error.Errors.Keys);
            Assert.Contains("TierPrices[0].MonthlyCostPerGiB", error.Errors.Keys);
        });
    }
}
