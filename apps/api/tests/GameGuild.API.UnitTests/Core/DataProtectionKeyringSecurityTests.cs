using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using GameGuild.API.Database;
using GameGuild.API.Setup;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.API.UnitTests.Core;

public sealed class DataProtectionKeyringSecurityTests
{
    private const string CertificateVariable = "DATAPROTECTION_CERTIFICATE_BASE64";
    private const string KeyVariable = "DATAPROTECTION_CERTIFICATE_KEY_BASE64";
    private const string Application = "GameGuild.OwnedKeyringNativeFixture";
    private const string Payload = "synthetic keyring fixture payload";

    [Theory]
    [InlineData("absent")]
    [InlineData("partial")]
    [InlineData("invalid-base64")]
    [InlineData("mismatched-key")]
    public void InvalidCertificate_RefusesDurableKeyRegistration(string kind)
    {
        using var material = CertificateMaterial.Create();
        using var wrongKey = RSA.Create(2048);
        var variables = kind switch
        {
            "absent" => new CertificateVariables(null, null),
            "partial" => new CertificateVariables(material.Certificate, null),
            "invalid-base64" => new CertificateVariables("!!!", "!!!"),
            "mismatched-key" => new CertificateVariables(material.Certificate,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(wrongKey.ExportPkcs8PrivateKeyPem()))),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var errors = new List<string>();
        Assert.Throws<InvalidOperationException>(() =>
            DataProtectionStartupConfiguration.ConfigureServices(new ServiceCollection(), Application, errors.Add, variables.Read));
    }

    [Fact]
    public async Task MissingCertificate_CannotCreateAPersistedPlaintextMasterKey()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("native_keyring_missing");
        await Initialize(database.ConnectionString);
        var variables = new CertificateVariables(null, null);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var provider = ProductionProvider(database.ConnectionString, variables.Read);
            _ = provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("owned-native-purpose").Protect(Payload);
            await using var context = Context(database.ConnectionString);
            _ = await context.DataProtectionKeys.CountAsync();
        });
        await using var verify = Context(database.ConnectionString);
        Assert.Equal(0, await verify.DataProtectionKeys.CountAsync());
    }

    [Fact]
    public async Task ValidCertificate_PersistsEncryptedMasterKeys_AndDecryptsAfterProviderRestart()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("native_keyring_certificate");
        await Initialize(database.ConnectionString);
        using var material = CertificateMaterial.Create();
        var variables = new CertificateVariables(material.Certificate, material.Key);
        string ciphertext;
        using (var first = ProductionProvider(database.ConnectionString, variables.Read))
        {
            ciphertext = first.GetRequiredService<IDataProtectionProvider>().CreateProtector("owned-native-purpose").Protect(Payload);
        }
        using var restarted = ProductionProvider(database.ConnectionString, variables.Read);
        Assert.Equal(Payload, restarted.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("owned-native-purpose").Unprotect(ciphertext));
        await AssertNoPlaintextMasterKeys(database.ConnectionString);
    }

    [Fact]
    public async Task ExistingPlaintextKey_IsProtectedWithoutLosingPreviouslyProtectedData()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("native_keyring_legacy");
        await Initialize(database.ConnectionString);
        string existing;
        var legacy = new ServiceCollection();
        legacy.AddLogging();
        legacy.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(database.ConnectionString));
        legacy.AddDataProtection().SetApplicationName(Application).PersistKeysToDbContext<ApplicationDbContext>();
        using (var provider = legacy.BuildServiceProvider())
        {
            // Explicit old configuration, solely to seed the isolated before-state.
            provider.GetRequiredService<IKeyManager>().CreateNewKey(
                DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30));
            existing = provider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("owned-native-purpose").Protect(Payload);
        }
        using var material = CertificateMaterial.Create();
        var variables = new CertificateVariables(material.Certificate, material.Key);
        using var secured = ProductionProvider(database.ConnectionString, variables.Read);
        Assert.Equal(Payload, secured.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("owned-native-purpose").Unprotect(existing));
        await AssertNoPlaintextMasterKeys(database.ConnectionString);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("weak-rsa")]
    [InlineData("non-rsa")]
    public void UnsuitableCertificate_FailsWithAFixedDiagnostic(string kind)
    {
        using var rsa = RSA.Create(kind == "weak-rsa" ? 1024 : 2048);
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = kind == "non-rsa"
            ? new CertificateRequest("CN=owned-native-unsuitable", ecdsa, HashAlgorithmName.SHA256)
            : new CertificateRequest("CN=owned-native-unsuitable", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var from = kind == "future" ? DateTimeOffset.UtcNow.AddDays(1) : DateTimeOffset.UtcNow.AddDays(-2);
        var until = kind == "expired" ? DateTimeOffset.UtcNow.AddDays(-1) : DateTimeOffset.UtcNow.AddDays(2);
        using var certificate = request.CreateSelfSigned(from, until);
        var variables = new CertificateVariables(
            Convert.ToBase64String(Encoding.UTF8.GetBytes(certificate.ExportCertificatePem())),
            Convert.ToBase64String(Encoding.UTF8.GetBytes(kind == "non-rsa"
                ? ecdsa.ExportPkcs8PrivateKeyPem() : rsa.ExportPkcs8PrivateKeyPem())));
        var errors = new List<string>();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DataProtectionStartupConfiguration.ConfigureServices(new ServiceCollection(), Application, errors.Add, variables.Read));
        Assert.Null(exception.InnerException);
        Assert.Equal("[DataProtection] The key-protection certificate is invalid or unavailable.", exception.Message);
        Assert.Single(errors);
    }

    [Fact]
    public async Task LegacyEncoding_ChangesWithoutMutatingCanonicalKeysOrRevocations()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("native_keyring_canonical");
        await Initialize(database.ConnectionString);
        var legacy = LegacyServices(database.ConnectionString);
        using (var provider = legacy.BuildServiceProvider())
        {
            var manager = provider.GetRequiredService<IKeyManager>();
            manager.CreateNewKey(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30));
            var expired = manager.CreateNewKey(DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1));
            manager.RevokeKey(expired.KeyId, "Synthetic expired-key retention control");
        }
        var before = await ReadRows(database.ConnectionString);
        using var material = CertificateMaterial.Create();
        var variables = new CertificateVariables(material.Certificate, material.Key);
        using var secured = ProductionProvider(database.ConnectionString, variables.Read);
        var repository = secured.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository!;
        var first = repository.GetAllElements();
        var second = repository.GetAllElements();
        Assert.Equal(before.Count, first.Count);
        foreach (var xml in before.Values)
        {
            Assert.True(first.Any(element => XElement.DeepEquals(XElement.Parse(xml), element)),
                "Repository conversion must preserve the canonical key and revocation XML.");
        }
        foreach (var element in first)
        {
            Assert.Contains(true, second.Select(value => XElement.DeepEquals(element, value)));
        }
        var after = await ReadRows(database.ConnectionString);
        Assert.True(before.Keys.Order().SequenceEqual(after.Keys.Order()));
        Assert.All(after.Values, xml => Assert.True(xml.Contains("protectedKey", StringComparison.Ordinal)));
        await AssertNoPlaintextMasterKeys(database.ConnectionString);
    }

    [Fact]
    public async Task CanonicalNamespacesAndWhitespace_ArePreservedAcrossProtectedReads()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("native_keyring_xml_identity");
        await Initialize(database.ConnectionString);
        using (var provider = LegacyServices(database.ConnectionString).BuildServiceProvider())
        {
            provider.GetRequiredService<IKeyManager>().CreateNewKey(
                DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30));
        }
        XElement canonical;
        await using (var context = Context(database.ConnectionString))
        {
            var row = await context.DataProtectionKeys.SingleAsync();
            canonical = XElement.Parse(row.Xml!, LoadOptions.PreserveWhitespace);
            canonical.SetAttributeValue("xmlns", string.Empty);
            canonical.SetAttributeValue(XNamespace.Xmlns + "owned", "urn:owned-native-namespace-control");
            canonical.AddFirst(new XComment("owned canonical identity control"), new XText("\n  "));
            row.Xml = canonical.ToString(SaveOptions.DisableFormatting);
            await context.SaveChangesAsync();
        }
        using var material = CertificateMaterial.Create();
        var variables = new CertificateVariables(material.Certificate, material.Key);
        using var secured = ProductionProvider(database.ConnectionString, variables.Read);
        var repository = secured.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository!;
        var first = repository.GetAllElements();
        var second = repository.GetAllElements();
        Assert.Single(first.Select(_ => true));
        Assert.Single(second.Select(_ => true));
        Assert.True(XElement.DeepEquals(canonical, first.Single()), "Initial conversion must preserve canonical XML identity.");
        Assert.True(XElement.DeepEquals(canonical, second.Single()), "Protected reads must preserve namespaces, comments and whitespace.");
        await AssertNoPlaintextMasterKeys(database.ConnectionString);
    }

    [Fact]
    public async Task WrongCertificate_RejectsExistingStorageWithoutOverwritingRows()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("native_keyring_wrong");
        await Initialize(database.ConnectionString);
        using var original = CertificateMaterial.Create();
        var originalVariables = new CertificateVariables(original.Certificate, original.Key);
        string ciphertext;
        using (var provider = ProductionProvider(database.ConnectionString, originalVariables.Read))
        {
            ciphertext = provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("owned-native-purpose").Protect(Payload);
        }
        var before = await ReadRows(database.ConnectionString);
        using var wrong = CertificateMaterial.Create();
        var wrongVariables = new CertificateVariables(wrong.Certificate, wrong.Key);
        using var restarted = ProductionProvider(database.ConnectionString, wrongVariables.Read);
        var repository = restarted.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository!;
        var exception = Assert.Throws<CryptographicException>(() => repository.GetAllElements());
        Assert.Equal("Data Protection key storage is invalid or unavailable.", exception.Message);
        Assert.Null(exception.InnerException);
        Assert.Throws<CryptographicException>(() => restarted.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("owned-native-purpose").Unprotect(ciphertext));
        var after = await ReadRows(database.ConnectionString);
        Assert.True(before.Count == after.Count && before.All(row => after.TryGetValue(row.Key, out var xml) && row.Value == xml),
            "An unavailable key must not replace or delete persisted keys.");
    }

    [Fact]
    public async Task InvalidLegacyRow_RollsBackAllEarlierConversions()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("native_keyring_atomic");
        await Initialize(database.ConnectionString);
        using (var provider = LegacyServices(database.ConnectionString).BuildServiceProvider())
        {
            provider.GetRequiredService<IKeyManager>().CreateNewKey(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30));
        }
        await using (var context = Context(database.ConnectionString))
        {
            context.DataProtectionKeys.Add(new Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey
                { FriendlyName = "synthetic malformed row", Xml = "<broken>" });
            await context.SaveChangesAsync();
        }
        var before = await ReadRows(database.ConnectionString);
        using var material = CertificateMaterial.Create();
        var variables = new CertificateVariables(material.Certificate, material.Key);
        using var secured = ProductionProvider(database.ConnectionString, variables.Read);
        var repository = secured.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository!;
        Assert.Throws<CryptographicException>(() => repository.GetAllElements());
        var after = await ReadRows(database.ConnectionString);
        Assert.True(before.Count == after.Count && before.All(row => after.TryGetValue(row.Key, out var xml) && row.Value == xml),
            "A failed conversion must preserve every persisted row.");
    }

    [Fact]
    public async Task ConcurrentProviders_PreserveLegacyKeyIdentityAndExistingPayload()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("native_keyring_concurrent");
        await Initialize(database.ConnectionString);
        string ciphertext;
        using (var provider = LegacyServices(database.ConnectionString).BuildServiceProvider())
        {
            provider.GetRequiredService<IKeyManager>().CreateNewKey(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30));
            ciphertext = provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("owned-native-purpose").Protect(Payload);
        }
        var before = await ReadRows(database.ConnectionString);
        using var material = CertificateMaterial.Create();
        var variables = new CertificateVariables(material.Certificate, material.Key);
        using var first = ProductionProvider(database.ConnectionString, variables.Read);
        using var second = ProductionProvider(database.ConnectionString, variables.Read);
        var repositories = new[] { first, second }.Select(provider =>
            provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository!).ToArray();
        var reads = await Task.WhenAll(repositories.Select(repository => Task.Run(repository.GetAllElements)));
        Assert.All(reads, elements => Assert.Equal(before.Count, elements.Count));
        foreach (var provider in new[] { first, second })
        {
            Assert.Equal(Payload, provider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("owned-native-purpose").Unprotect(ciphertext));
        }
        var after = await ReadRows(database.ConnectionString);
        Assert.True(before.Keys.Order().SequenceEqual(after.Keys.Order()));
        await AssertNoPlaintextMasterKeys(database.ConnectionString);
    }

    private static ServiceCollection LegacyServices(string connection)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connection));
        services.AddDataProtection().SetApplicationName(Application).PersistKeysToDbContext<ApplicationDbContext>();
        return services;
    }

    private static async Task<Dictionary<int, string>> ReadRows(string connection)
    {
        await using var context = Context(connection);
        return await context.DataProtectionKeys.AsNoTracking().ToDictionaryAsync(row => row.Id, row => row.Xml!);
    }

    private static ServiceProvider ProductionProvider(string connection, Func<string, string?> environmentReader)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connection));
        DataProtectionStartupConfiguration.ConfigureServices(services, Application, _ => { }, environmentReader);
        return services.BuildServiceProvider();
    }

    private static ApplicationDbContext Context(string connection) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options);

    private static async Task Initialize(string connection)
    {
        await using var context = Context(connection);
        await context.Database.EnsureCreatedAsync();
    }

    private static async Task AssertNoPlaintextMasterKeys(string connection)
    {
        await using var context = Context(connection);
        var rows = await context.DataProtectionKeys.AsNoTracking().ToListAsync();
        Assert.NotEmpty(rows);
        // Assert on a boolean so no serialized key material appears in failure output or evidence.
        var plaintext = rows.Any(row => row.Xml is not null &&
            XElement.Parse(row.Xml).Descendants("masterKey")
                .Any(element => element.Elements("value").Any(value => !string.IsNullOrWhiteSpace(value.Value))));
        Assert.False(plaintext, "The durable key repository contains an unencrypted recoverable master key.");
    }

    private sealed class CertificateVariables(string? certificate, string? key)
    {
        public string? Read(string name) => name == CertificateVariable ? certificate : name == KeyVariable ? key : null;
    }

    private sealed class CertificateMaterial : IDisposable
    {
        private readonly RSA _rsa = RSA.Create(2048);
        private readonly X509Certificate2 _certificate;
        public string Certificate { get; }
        public string Key { get; }
        private CertificateMaterial()
        {
            var request = new CertificateRequest("CN=owned-native-keyring", _rsa,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            _certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
            Certificate = Convert.ToBase64String(Encoding.UTF8.GetBytes(_certificate.ExportCertificatePem()));
            Key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_rsa.ExportPkcs8PrivateKeyPem()));
        }
        public static CertificateMaterial Create() => new();
        public void Dispose()
        {
            _certificate.Dispose();
            _rsa.Dispose();
        }
    }
}
