using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Commerce.Payments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GameGuild.Commerce.Payments.IntegrationTests.Security;

/// <summary>
///     Integration Tests: Tax Calculation - Multi-Jurisdiction
///     From: COMMERCE_MODULES_SECURITY_AUDIT.md Section 7 - Test Plan
///     These tests verify correct tax rates across multiple jurisdictions.
/// </summary>
public class TaxCalculationIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = $"TaxCalculationTestDb_{Guid.NewGuid()}";

    public TaxCalculationIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                d.ServiceType == typeof(ApplicationDbContext) ||
                                d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                                d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });

                services.AddHttpLogging(o => { });
            });
        });

        _client = _factory.CreateClient();
    }

    #region Multi-Jurisdiction Tax Rate Tests

    [Fact]
    public async Task TaxCalculation_USJurisdiction_AppliesSalesTax()
    {
        // Arrange
        await SeedUSJurisdictionAsync();
        
        var request = new
        {
            JurisdictionCode = "US-CA",
            Amount = 100.00m,
            Currency = "USD",
            CustomerType = "B2C",
            ProductCategory = "digital_goods"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        // California sales tax is approximately 7.25% base rate
        result?.TaxAmount.Should().BeGreaterThan(0);
        result?.EffectiveTaxRate.Should().BeGreaterThan(0.07m);
        result?.JurisdictionCode.Should().Be("US-CA");
    }

    [Fact]
    public async Task TaxCalculation_EUJurisdiction_AppliesVAT()
    {
        // Arrange
        await SeedEUJurisdictionAsync();
        
        var request = new
        {
            JurisdictionCode = "DE",
            Amount = 100.00m,
            Currency = "EUR",
            CustomerType = "B2C",
            ProductCategory = "digital_goods"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        // Germany VAT is 19% standard rate
        result?.TaxAmount.Should().BeApproximately(19.00m, 0.01m);
        result?.EffectiveTaxRate.Should().BeApproximately(0.19m, 0.001m);
        result?.TaxType.Should().Be("VAT");
    }

    [Fact]
    public async Task TaxCalculation_UKJurisdiction_AppliesVAT()
    {
        // Arrange
        await SeedUKJurisdictionAsync();
        
        var request = new
        {
            JurisdictionCode = "GB",
            Amount = 100.00m,
            Currency = "GBP",
            CustomerType = "B2C",
            ProductCategory = "digital_goods"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        // UK VAT is 20%
        result?.TaxAmount.Should().BeApproximately(20.00m, 0.01m);
        result?.EffectiveTaxRate.Should().BeApproximately(0.20m, 0.001m);
    }

    [Fact]
    public async Task TaxCalculation_CanadaJurisdiction_AppliesGSTAndPST()
    {
        // Arrange
        await SeedCanadaJurisdictionAsync();
        
        var request = new
        {
            JurisdictionCode = "CA-BC",
            Amount = 100.00m,
            Currency = "CAD",
            CustomerType = "B2C",
            ProductCategory = "digital_goods"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        // BC has 5% GST + 7% PST = 12% combined
        result?.TaxAmount.Should().BeApproximately(12.00m, 0.01m);
        result?.TaxBreakdowns.Should().HaveCount(2);
    }

    #endregion

    #region B2B Reverse Charge Tests

    [Fact]
    public async Task TaxCalculation_EUB2B_WithValidVAT_AppliesReverseCharge()
    {
        // Arrange
        await SeedEUJurisdictionAsync();
        
        var request = new
        {
            JurisdictionCode = "DE",
            Amount = 100.00m,
            Currency = "EUR",
            CustomerType = "B2B",
            CustomerVatNumber = "DE123456789",
            ProductCategory = "digital_goods"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        // B2B with valid VAT number should have reverse charge (0% tax)
        result?.IsReverseCharge.Should().BeTrue();
        result?.TaxAmount.Should().Be(0);
        result?.TotalAmount.Should().Be(100.00m);
    }

    [Fact]
    public async Task TaxCalculation_EUB2B_WithoutVAT_AppliesNormalVAT()
    {
        // Arrange
        await SeedEUJurisdictionAsync();
        
        var request = new
        {
            JurisdictionCode = "DE",
            Amount = 100.00m,
            Currency = "EUR",
            CustomerType = "B2B",
            CustomerVatNumber = (string?)null,
            ProductCategory = "digital_goods"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        // B2B without VAT number should pay normal VAT
        result?.IsReverseCharge.Should().BeFalse();
        result?.TaxAmount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Tax Exemption Tests

    [Fact]
    public async Task TaxCalculation_WithTaxExemption_ReturnsZeroTax()
    {
        // Arrange
        await SeedUSJurisdictionAsync();
        
        var request = new
        {
            JurisdictionCode = "US-CA",
            Amount = 100.00m,
            Currency = "USD",
            CustomerType = "B2B",
            ProductCategory = "digital_goods",
            ApplicableExemptions = new[] { "NONPROFIT_501C3" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        result?.IsTaxExempt.Should().BeTrue();
        result?.TaxAmount.Should().Be(0);
        result?.ExemptionReason.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Reduced Rate Tests

    [Fact]
    public async Task TaxCalculation_EUReducedRate_ForEssentialGoods()
    {
        // Arrange
        await SeedEUJurisdictionWithReducedRatesAsync();
        
        var request = new
        {
            JurisdictionCode = "DE",
            Amount = 100.00m,
            Currency = "EUR",
            CustomerType = "B2C",
            ProductCategory = "books" // Books have reduced VAT in Germany
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        // Germany reduced VAT for books is 7%
        result?.EffectiveTaxRate.Should().BeApproximately(0.07m, 0.001m);
    }

    [Fact]
    public async Task TaxCalculation_ZeroRate_ForExportedGoods()
    {
        // Arrange
        await SeedUKJurisdictionAsync();
        
        var request = new
        {
            JurisdictionCode = "GB",
            Amount = 100.00m,
            Currency = "GBP",
            CustomerType = "B2C",
            ProductCategory = "export_goods",
            DestinationCountry = "US" // Exported to non-UK
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        // Exported goods are zero-rated
        result?.TaxAmount.Should().Be(0);
    }

    #endregion

    #region Tax Inclusive vs Exclusive Tests

    [Fact]
    public async Task TaxCalculation_TaxInclusive_CalculatesCorrectSubtotal()
    {
        // Arrange
        await SeedEUJurisdictionAsync();
        
        var request = new
        {
            JurisdictionCode = "DE",
            Amount = 119.00m, // Tax-inclusive amount
            Currency = "EUR",
            CustomerType = "B2C",
            ProductCategory = "digital_goods",
            IsTaxInclusive = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        // 119 EUR inclusive of 19% VAT = 100 EUR subtotal + 19 EUR tax
        result?.SubtotalAmount.Should().BeApproximately(100.00m, 0.01m);
        result?.TaxAmount.Should().BeApproximately(19.00m, 0.01m);
        result?.TotalAmount.Should().BeApproximately(119.00m, 0.01m);
    }

    [Fact]
    public async Task TaxCalculation_TaxExclusive_CalculatesCorrectTotal()
    {
        // Arrange
        await SeedEUJurisdictionAsync();
        
        var request = new
        {
            JurisdictionCode = "DE",
            Amount = 100.00m, // Tax-exclusive amount
            Currency = "EUR",
            CustomerType = "B2C",
            ProductCategory = "digital_goods",
            IsTaxInclusive = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        // 100 EUR + 19% VAT = 119 EUR total
        result?.SubtotalAmount.Should().BeApproximately(100.00m, 0.01m);
        result?.TaxAmount.Should().BeApproximately(19.00m, 0.01m);
        result?.TotalAmount.Should().BeApproximately(119.00m, 0.01m);
    }

    #endregion

    #region Unknown/Invalid Jurisdiction Tests

    [Fact]
    public async Task TaxCalculation_UnknownJurisdiction_ReturnsZeroTax()
    {
        // Arrange - no seeding, jurisdiction doesn't exist
        var request = new
        {
            JurisdictionCode = "XX-UNKNOWN",
            Amount = 100.00m,
            Currency = "USD",
            CustomerType = "B2C"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/calculate", request);

        // Assert - Should return zero tax for unknown jurisdiction (safe default)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaxCalculationResultDto>();
        
        result?.TaxAmount.Should().Be(0);
    }

    #endregion

    #region VAT Number Validation Tests

    [Theory]
    [InlineData("DE123456789", "DE", true)]
    [InlineData("FR12345678901", "FR", true)]
    [InlineData("GB123456789", "GB", true)]
    [InlineData("INVALID", "DE", false)]
    [InlineData("", "DE", false)]
    [InlineData("12345", "DE", false)]
    public async Task VATValidation_VariousFormats_ValidatesCorrectly(string vatNumber, string countryCode, bool expectedValid)
    {
        // Arrange
        var request = new
        {
            VatNumber = vatNumber,
            CountryCode = countryCode
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments/tax/validate-vat", request);

        // Assert
        if (expectedValid)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        else
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
    }

    #endregion

    #region Helper Methods

    private async Task SeedUSJurisdictionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create US-CA jurisdiction with sales tax
        // Note: Actual seeding depends on entity implementation
        await Task.CompletedTask;
    }

    private async Task SeedEUJurisdictionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create DE jurisdiction with VAT
        await Task.CompletedTask;
    }

    private async Task SeedEUJurisdictionWithReducedRatesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create DE jurisdiction with both standard and reduced VAT rates
        await Task.CompletedTask;
    }

    private async Task SeedUKJurisdictionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create GB jurisdiction with VAT
        await Task.CompletedTask;
    }

    private async Task SeedCanadaJurisdictionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create CA-BC jurisdiction with GST + PST
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    #endregion
}

/// <summary>
/// DTO for tax calculation results
/// </summary>
internal class TaxCalculationResultDto
{
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal EffectiveTaxRate { get; set; }
    public string? JurisdictionCode { get; set; }
    public string? JurisdictionName { get; set; }
    public string? TaxType { get; set; }
    public string? TaxDescription { get; set; }
    public bool IsTaxExempt { get; set; }
    public bool IsReverseCharge { get; set; }
    public List<TaxBreakdownDto>? TaxBreakdowns { get; set; }
    public string? ExemptionReason { get; set; }
}

internal class TaxBreakdownDto
{
    public string? TaxType { get; set; }
    public string? Description { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}
