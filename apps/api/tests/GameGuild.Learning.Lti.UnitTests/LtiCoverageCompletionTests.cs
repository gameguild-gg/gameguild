using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GameGuild.Learning.Grading.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace GameGuild.Learning.Lti.Tests;

public class LtiEntityValidationTests
{
    [Theory]
    [InlineData("issuer")]
    [InlineData("clientId")]
    [InlineData("deploymentId")]
    [InlineData("authTokenUrl")]
    [InlineData("platformJwksUrl")]
    [InlineData("authorizationUrl")]
    [InlineData("keyId")]
    [InlineData("privateKeyPem")]
    public void DeploymentCreate_RejectsEveryBlankRequiredValue(string field)
    {
        var values = new Dictionary<string, string>
        {
            ["issuer"] = "https://canvas.test",
            ["clientId"] = "client-1",
            ["deploymentId"] = "deployment-1",
            ["authTokenUrl"] = "https://canvas.test/token",
            ["platformJwksUrl"] = "https://canvas.test/jwks",
            ["authorizationUrl"] = "https://canvas.test/authorize",
            ["keyId"] = "tool-key-1",
            ["privateKeyPem"] = "private-key"
        };
        values[field] = " ";

        var action = () => LtiDeployment.Create(
            values["issuer"],
            values["clientId"],
            values["deploymentId"],
            values["authTokenUrl"],
            values["platformJwksUrl"],
            values["authorizationUrl"],
            values["keyId"],
            values["privateKeyPem"]);

        action.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(field);
    }

    [Theory]
    [InlineData("lineItemId")]
    [InlineData("lineItemUrl")]
    [InlineData("maxScore")]
    public void LineItemCreate_RejectsEveryInvalidRequiredValue(string field)
    {
        var action = () => LtiLineItemMapping.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            field == "lineItemId" ? " " : "line-1",
            field == "lineItemUrl" ? " " : "https://canvas.test/line-items/1",
            field == "maxScore" ? ScoreValue.FromUnits(0) : ScoreValue.FromUnits(100));

        action.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(field);
    }

    [Fact]
    public void UserMappingCreate_RejectsBlankSubject()
    {
        var action = () => LtiUserMapping.Create(Guid.NewGuid(), Guid.NewGuid(), " ");

        action.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("sub");
    }
}

public class LtiLaunchStateStoreCoverageTests
{
    [Fact]
    public void TryConsume_RejectsMissingStateAndMissingNonceIndependently()
    {
        var store = new LtiLaunchStateStore();
        var deploymentId = Guid.NewGuid();
        var issued = store.Issue(deploymentId);

        store.TryConsume(string.Empty, issued.Nonce, deploymentId).Should().BeFalse();
        store.TryConsume(issued.State, string.Empty, deploymentId).Should().BeFalse();
    }

    [Fact]
    public void TryConsume_RejectsWrongDeploymentAndWrongNonce()
    {
        var store = new LtiLaunchStateStore();
        var deploymentId = Guid.NewGuid();
        var wrongDeploymentState = store.Issue(deploymentId);
        var wrongNonceState = store.Issue(deploymentId);

        store.TryConsume(wrongDeploymentState.State, wrongDeploymentState.Nonce, Guid.NewGuid())
            .Should().BeFalse();
        store.TryConsume(wrongNonceState.State, "wrong-nonce", deploymentId)
            .Should().BeFalse();
    }

    [Fact]
    public void TryConsume_RejectsAnExpiredEntry()
    {
        var store = new LtiLaunchStateStore();
        var deploymentId = Guid.NewGuid();
        const string state = "expired-state";
        const string nonce = "expired-nonce";
        AddEntry(store, state, deploymentId, nonce, DateTimeOffset.UtcNow.AddMinutes(-1));

        store.TryConsume(state, nonce, deploymentId).Should().BeFalse();
    }

    [Fact]
    public void Issue_PrunesExpiredEntries()
    {
        var store = new LtiLaunchStateStore();
        const string expiredState = "state-to-prune";
        AddEntry(
            store,
            expiredState,
            Guid.NewGuid(),
            "expired-nonce",
            DateTimeOffset.UtcNow.AddMinutes(-1));

        store.Issue(Guid.NewGuid());

        ContainsEntry(store, expiredState).Should().BeFalse();
    }

    private static void AddEntry(
        LtiLaunchStateStore store,
        string state,
        Guid deploymentId,
        string nonce,
        DateTimeOffset expiresAt)
    {
        var entries = GetEntries(store);
        var entryType = typeof(LtiLaunchStateStore)
            .GetNestedType("Entry", BindingFlags.NonPublic)!;
        var entry = Activator.CreateInstance(
            entryType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [deploymentId, nonce, expiresAt],
            culture: null)!;
        var added = (bool)entries.GetType().GetMethod("TryAdd")!
            .Invoke(entries, [state, entry])!;
        added.Should().BeTrue();
    }

    private static bool ContainsEntry(LtiLaunchStateStore store, string state)
    {
        var entries = GetEntries(store);
        return (bool)entries.GetType().GetMethod("ContainsKey")!
            .Invoke(entries, [state])!;
    }

    private static object GetEntries(LtiLaunchStateStore store) =>
        typeof(LtiLaunchStateStore)
            .GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(store)!;
}

public class LtiPlatformJwksServiceCoverageTests
{
    [Theory]
    [InlineData("{\"keys\":[]}")]
    [InlineData("{}")]
    public async Task ValidateIdToken_WhenJwksContainsNoUsableKeys_ReturnsNull(string jwks)
    {
        var service = CreateService(jwks);

        var principal = await service.ValidateIdTokenAsync("not-a-token", CreateDeployment());

        principal.Should().BeNull();
    }

    [Fact]
    public async Task ValidateIdToken_WhenTokenCannotBeRead_ReturnsNullAndCachesJwks()
    {
        using var rsa = RSA.Create(2048);
        var handler = new CapturingHandler(_ => JsonResponse(SerializeJwks([Jwk(rsa, "key-1")])));
        var service = new LtiPlatformJwksService(
            new StubHttpClientFactory(new HttpClient(handler)),
            NullLogger<LtiPlatformJwksService>.Instance);
        var deployment = CreateDeployment();

        (await service.ValidateIdTokenAsync("not-a-token", deployment)).Should().BeNull();
        (await service.ValidateIdTokenAsync("still-not-a-token", deployment)).Should().BeNull();

        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task ValidateIdToken_SkipsMalformedJwksEntriesAndUsesTheValidRsaKey()
    {
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(false);
        var modulus = Base64UrlEncoder.Encode(parameters.Modulus!);
        var exponent = Base64UrlEncoder.Encode(parameters.Exponent!);
        object[] keys =
        [
            new Dictionary<string, object?> { ["kid"] = "missing-kty" },
            new Dictionary<string, object?> { ["kty"] = "EC", ["kid"] = "ec" },
            new Dictionary<string, object?> { ["kty"] = "RSA", ["n"] = modulus, ["e"] = exponent },
            new Dictionary<string, object?> { ["kty"] = "RSA", ["kid"] = "missing-n", ["e"] = exponent },
            new Dictionary<string, object?> { ["kty"] = "RSA", ["kid"] = "missing-e", ["n"] = modulus },
            new Dictionary<string, object?> { ["kty"] = "RSA", ["kid"] = 42, ["n"] = modulus, ["e"] = exponent },
            Jwk(rsa, "key-1")
        ];
        var service = CreateService(SerializeJwks(keys));
        var deployment = CreateDeployment();
        var token = CreateSignedToken(rsa, "key-1", deployment);

        var principal = await service.ValidateIdTokenAsync(token, deployment);

        principal.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateIdToken_WithMultipleKeys_ResolvesTheSigningKeyByKid()
    {
        using var signingKey = RSA.Create(2048);
        using var otherKey = RSA.Create(2048);
        var service = CreateService(SerializeJwks(
        [
            Jwk(otherKey, "other-key"),
            Jwk(signingKey, "signing-key")
        ]));
        var deployment = CreateDeployment();
        var token = CreateSignedToken(signingKey, "signing-key", deployment);

        var principal = await service.ValidateIdTokenAsync(token, deployment);

        principal.Should().NotBeNull();
    }

    private static LtiPlatformJwksService CreateService(string json)
    {
        var handler = new CapturingHandler(_ => JsonResponse(json));
        return new LtiPlatformJwksService(
            new StubHttpClientFactory(new HttpClient(handler)),
            NullLogger<LtiPlatformJwksService>.Instance);
    }

    private static LtiDeployment CreateDeployment() =>
        LtiDeployment.Create(
            "https://canvas.test",
            "client-1",
            "deployment-1",
            "https://canvas.test/token",
            "https://canvas.test/jwks",
            "https://canvas.test/authorize",
            "tool-key-1",
            "private-key");

    private static Dictionary<string, object?> Jwk(RSA rsa, string kid)
    {
        var parameters = rsa.ExportParameters(false);
        return new Dictionary<string, object?>
        {
            ["kty"] = "RSA",
            ["kid"] = kid,
            ["n"] = Base64UrlEncoder.Encode(parameters.Modulus!),
            ["e"] = Base64UrlEncoder.Encode(parameters.Exponent!)
        };
    }

    private static string SerializeJwks(IEnumerable<object> keys) =>
        JsonSerializer.Serialize(new Dictionary<string, object?> { ["keys"] = keys });

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static string CreateSignedToken(RSA rsa, string kid, LtiDeployment deployment)
    {
        var now = DateTime.UtcNow;
        return new JwtSecurityTokenHandler().CreateEncodedJwt(
            deployment.Issuer,
            deployment.ClientId,
            new ClaimsIdentity([new Claim("sub", "learner-1")]),
            now.AddMinutes(-1),
            now.AddMinutes(10),
            now,
            new SigningCredentials(
                new RsaSecurityKey(rsa) { KeyId = kid },
                SecurityAlgorithms.RsaSha256));
    }
}
