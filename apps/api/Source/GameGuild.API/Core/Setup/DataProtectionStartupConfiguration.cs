using System.Security.Cryptography.X509Certificates;
using System.Text;
using GameGuild.API.Database;
using Microsoft.AspNetCore.DataProtection;

namespace GameGuild.API.Setup;

internal static class DataProtectionStartupConfiguration
{
    public static void Configure(WebApplicationBuilder builder, IApiProductComposition productComposition)
    {
        ConfigureServices(
            builder.Services,
            productComposition.ApplicationName,
            Console.Error.WriteLine);
    }

    internal static void ConfigureServices(
        IServiceCollection services,
        string applicationName,
        Action<string> writeError)
    {
        var builder = services.AddDataProtection()
            .SetApplicationName(applicationName)
            .PersistKeysToDbContext<ApplicationDbContext>();

        var certificate = LoadCertificate(Environment.GetEnvironmentVariable, writeError);
        if (certificate is not null)
        {
            builder.ProtectKeysWithCertificate(certificate);
        }
    }

    internal static X509Certificate2? LoadCertificate(
        Func<string, string?> environmentReader,
        Action<string> writeError)
    {
        var certificateBase64 = environmentReader("DATAPROTECTION_CERTIFICATE_BASE64");
        var keyBase64 = environmentReader("DATAPROTECTION_CERTIFICATE_KEY_BASE64");

        if (string.IsNullOrWhiteSpace(certificateBase64) || string.IsNullOrWhiteSpace(keyBase64))
        {
            writeError(
                "[DataProtection] DATAPROTECTION_CERTIFICATE_BASE64 / DATAPROTECTION_CERTIFICATE_KEY_BASE64 are not both set; keys will be stored unencrypted.");
            return null;
        }

        try
        {
            var certificatePem = Encoding.UTF8.GetString(Convert.FromBase64String(certificateBase64));
            var keyPem = Encoding.UTF8.GetString(Convert.FromBase64String(keyBase64));
            return X509Certificate2.CreateFromPem(certificatePem, keyPem);
        }
        catch (Exception exception)
        {
            writeError(
                $"[DataProtection] Failed to load the key-protection certificate from environment variables: {exception.Message}. Keys will be stored unencrypted.");
            return null;
        }
    }
}
