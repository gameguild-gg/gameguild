using System.Net.Http.Json;
using GameGuild.API.Database;
using GameGuild.Identity.Tenants;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace GameGuild.Tests.Authentication.Integration;

/// <summary>
///     Issue #268 acceptance, review item 1 (browser flow): the complete
///     SP-initiated HTTP-POST flow against a configured test identity provider —
///     <see cref="TestSamlIdentityProvider" /> mints genuinely signed SAML artefacts
///     (self-signed X.509 + XML-DSig RSA-SHA256) and parses the deflated
///     <c>SAMLRequest</c> exactly like a real IdP; nothing about the transport or the
///     signatures is mocked. The browser leg posts through the real HTTP ACS endpoint
///     and must end in an authenticated session. The PostgreSQL-backed protocol-state
///     store is exercised across two API hosts sharing one database (review item 3's
///     cross-instance evidence), and the state-binding/correlation refusals (review
///     items 2 and 4) are proven end-to-end: a refused callback never burns the
///     pending request, so the legitimate browser still completes afterwards.
/// </summary>
public sealed class SamlBrowserFlowPostgreSqlTests : IAsyncLifetime
{
    private EconomyPostgreSqlTestDatabase _database = null!;
    private TestSamlIdentityProvider _idp = null!;
    private WebApplicationFactory<GameGuild.API.Program> _factory = null!;
    private WebApplicationFactory<GameGuild.API.Program> _secondFactory = null!;
    private HttpClient _client = null!;
    private HttpClient _secondClient = null!;

    public async Task InitializeAsync()
    {
        _database = await EconomyPostgreSqlTestDatabase.CreateAsync("saml_browser_flows");
        await ApplyMigrationsAsync(_database.ConnectionString);

        _idp = new TestSamlIdentityProvider();
        _factory = CreateFactory(_idp.SigningCertificateBase64);
        _secondFactory = CreateFactory(_idp.SigningCertificateBase64);
        _client = _factory.CreateClient();
        _secondClient = _secondFactory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await dbContext.Set<Tenant>().AnyAsync(tenant => tenant.IsDefault))
            {
                dbContext.Add(new Tenant
                {
                    Name = "Public tenant",
                    Slug = "public",
                    AdminEmail = "admin@public.test",
                    IsDefault = true,
                    IsActive = true
                });
                await dbContext.SaveChangesAsync();
            }
        }
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _secondClient?.Dispose();
        _factory?.Dispose();
        _secondFactory?.Dispose();
        _idp?.Dispose();

        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    private WebApplicationFactory<GameGuild.API.Program> CreateFactory(string signingCertificate) =>
        new SamlBrowserFlowApiFactory(_database.ConnectionString, signingCertificate);

    /// <summary>Starts a sign-in like the browser does and answers it like the test IdP does.</summary>
    private async Task<(TestSamlIdentityProvider.ParsedAuthnRequest Request, string RelayState, string Response)> StartFlowAndBuildResponseAsync(
        TestSamlIdentityProvider.ResponseOptions? options = null,
        HttpClient? client = null)
    {
        var initiator = client ?? _client;
        using var authorize = await initiator.PostAsJsonAsync(
            $"/v1/auth/saml/{TestSamlIdentityProvider.Slug}:sign-in-authorize",
            new { });
        authorize.EnsureSuccessStatusCode();
        var challenge = await authorize.Content.ReadFromJsonAsync<SamlChallengeResponse>();
        Assert.NotNull(challenge);

        var request = _idp.ParseAuthnRequest(challenge.RedirectUrl);
        Assert.Equal(TestSamlIdpSpEntityId, request.Issuer);
        Assert.Equal(TestSamlIdentityProvider.AcsUrl, request.AssertionConsumerServiceUrl);

        var response = _idp.BuildSignedResponse(request, challenge.RelayState, options);
        return (request, challenge.RelayState, response);
    }

    private async Task<HttpResponseMessage> PostCallbackAsync(string response, string relayState, HttpClient? client = null) =>
        await (client ?? _client).PostAsJsonAsync(
            $"/v1/auth/saml/{TestSamlIdentityProvider.Slug}:assertion-consumer",
            new { samlResponse = response, relayState });

    private const string TestSamlIdpSpEntityId = TestSamlIdentityProvider.SpEntityId;

    private sealed record SamlChallengeResponse(string RedirectUrl, string RequestId, string RelayState);

    [Fact]
    public async Task BrowserPostBackFlow_SignsInAndReturnsWorkingSession()
    {
        var (_, relayState, samlResponse) = await StartFlowAndBuildResponseAsync();

        using var callback = await PostCallbackAsync(samlResponse, relayState);
        var body = await callback.Content.ReadAsStringAsync();
        Assert.True(callback.IsSuccessStatusCode, $"ACS status {(int)callback.StatusCode}: {body}");

        var signIn = await callback.Content.ReadFromJsonAsync<SamlSignInResponseBody>();
        Assert.NotNull(signIn);
        Assert.True(signIn.Success);
        Assert.False(string.IsNullOrWhiteSpace(signIn.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(signIn.RefreshToken));
        Assert.Equal(TestSamlIdentityProvider.SubjectEmail, signIn.Email);

        // The issued access token must carry a real authenticated session.
        using var sessionRequest = new HttpRequestMessage(HttpMethod.Get, "/v1/auth/session-state");
        sessionRequest.Headers.Authorization = new("Bearer", signIn.AccessToken);
        using var authenticated = await _client.SendAsync(sessionRequest);
        Assert.True(
            authenticated.IsSuccessStatusCode,
            $"session-state returned {(int)authenticated.StatusCode}: {await authenticated.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task Callback_WithForeignRelayState_RefusedAndLegitimateBrowserStillCompletes()
    {
        var (_, relayState, samlResponse) = await StartFlowAndBuildResponseAsync();

        using var foreign = await PostCallbackAsync(samlResponse, "attacker-chosen-relay-state");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, foreign.StatusCode);

        // A foreign RelayState never consumes the pending request: the legitimate
        // browser, holding the RelayState that was actually issued, still signs in.
        using var legitimate = await PostCallbackAsync(samlResponse, relayState);
        Assert.True(legitimate.IsSuccessStatusCode,
            $"legitimate callback refused {(int)legitimate.StatusCode}: {await legitimate.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task Callback_WithoutRelayState_Refused()
    {
        var (_, _, samlResponse) = await StartFlowAndBuildResponseAsync();

        using var missing = await PostCallbackAsync(samlResponse, string.Empty);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, missing.StatusCode);
    }

    [Fact]
    public async Task Callback_ReplayedResponse_Refused()
    {
        var (_, relayState, samlResponse) = await StartFlowAndBuildResponseAsync();

        using var first = await PostCallbackAsync(samlResponse, relayState);
        Assert.True(first.IsSuccessStatusCode);

        using var replay = await PostCallbackAsync(samlResponse, relayState);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Callback_SubjectCorrelationMismatch_RefusedWithoutBurningPendingRequest()
    {
        // Review item 4: a genuinely assertion-signed response re-wrapped in a forged
        // unsigned outer response carrying a fresh InResponseTo (the attacker's own
        // AuthnRequest). The signed subject confirmation names the original request,
        // so the mismatch must be refused before any single-use state is spent.
        var (originalRequest, originalRelayState, _) = await StartFlowAndBuildResponseAsync();

        // The attacker starts their own AuthnRequest (fresh InResponseTo + RelayState)
        // and wraps the victim's assertion-signed response in an unsigned envelope
        // claiming it: outer InResponseTo = attacker's request, signed subject
        // confirmation = the original request.
        var (attackerRequest, attackerRelayState, forgedResponse) = await StartFlowAndBuildResponseAsync(
            new TestSamlIdentityProvider.ResponseOptions(
                SubjectInResponseTo: originalRequest.Id,
                SignResponse: false));
        Assert.NotEqual(originalRequest.Id, attackerRequest.Id);

        using var forged = await PostCallbackAsync(forgedResponse, attackerRelayState);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, forged.StatusCode);

        // Neither the refused forgery nor anything else consumed the original flow's
        // pending request: the legitimate browser still completes it.
        var legitimateResponse = _idp.BuildSignedResponse(
            originalRequest,
            originalRelayState,
            TestSamlIdentityProvider.ResponseOptions.ForRequest(originalRequest));
        using var legitimate = await PostCallbackAsync(legitimateResponse, originalRelayState);
        Assert.True(legitimate.IsSuccessStatusCode,
            $"legitimate callback refused {(int)legitimate.StatusCode}: {await legitimate.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task Callback_SameResponseRacedAcrossTwoApiHosts_ExactlyOneSucceeds()
    {
        // Review item 3: two application instances share the database; the same
        // (genuinely signed) response posted concurrently to both must authenticate
        // exactly once — the loser is refused on single-use state.
        var (_, relayState, samlResponse) = await StartFlowAndBuildResponseAsync();

        var firstPost = PostCallbackAsync(samlResponse, relayState, _client);
        var secondPost = PostCallbackAsync(samlResponse, relayState, _secondClient);
        var results = await Task.WhenAll(firstPost, secondPost);

        using var _ = results[0];
        using var __ = results[1];
        Assert.Equal(1, results.Count(response => response.StatusCode == System.Net.HttpStatusCode.OK));
        Assert.Equal(1, results.Count(response => response.StatusCode == System.Net.HttpStatusCode.Unauthorized));
    }

    [Fact]
    public async Task SignIn_MfaAttestedAssertion_SurfacesProviderMfaAndLogoutUrlIsServed()
    {
        // Review item 5 (behavior retention): the assertion's AuthnContext still
        // reaches the response and the configured IdP logout path is served.
        var (_, relayState, samlResponse) = await StartFlowAndBuildResponseAsync(
            new TestSamlIdentityProvider.ResponseOptions(AuthnContextClassRef: TestSamlIdentityProvider.MfaAuthnContext));

        using var callback = await PostCallbackAsync(samlResponse, relayState);
        Assert.True(callback.IsSuccessStatusCode,
            $"callback refused {(int)callback.StatusCode}: {await callback.Content.ReadAsStringAsync()}");
        var signIn = await callback.Content.ReadFromJsonAsync<SamlSignInResponseBody>();
        Assert.NotNull(signIn);
        Assert.True(signIn.MfaVerifiedByProvider, "a TimeSyncToken AuthnContext must surface as provider MFA proof");
        Assert.Equal(TestSamlIdentityProvider.MfaAuthnContext, signIn.AuthenticationContextClassReference);

        using var logoutUrl = await _client.GetAsync(
            $"/v1/auth/saml/{TestSamlIdentityProvider.Slug}:logout-url?postLogoutRedirectUri=https%3A%2F%2Fgameguild.example.test%2Fsigned-out");
        Assert.True(logoutUrl.IsSuccessStatusCode);
        var logout = await logoutUrl.Content.ReadFromJsonAsync<SamlLogoutUrlResponseBody>();
        Assert.NotNull(logout);
        Assert.StartsWith(TestSamlIdentityProvider.IdpSingleLogoutUrl, logout.LogoutUrl);
    }

    private sealed record SamlSignInResponseBody(
        bool Success,
        string AccessToken,
        string RefreshToken,
        string Email,
        bool MfaVerifiedByProvider,
        string? AuthenticationContextClassReference);

    private sealed record SamlLogoutUrlResponseBody(string LogoutUrl);

    private async Task ApplyMigrationsAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    private sealed class SamlBrowserFlowApiFactory(string connectionString, string signingCertificate)
        : WebApplicationFactory<GameGuild.API.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var connection = new NpgsqlConnectionStringBuilder(connectionString);

            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["ConnectionStrings:MigrationConnection"] = connectionString,
                    ["Database:FailStartupOnMigrationFailure"] = "true",
                    ["Database:GrantRuntimeRoleAfterMigrations"] = "false",
                    ["Database:RunStartupInitialization"] = "false",
                    ["POSTGRES_HOST"] = connection.Host,
                    ["POSTGRES_PORT"] = connection.Port.ToString(),
                    ["POSTGRES_DB"] = connection.Database,
                    ["POSTGRES_USER"] = connection.Username,
                    ["POSTGRES_PASSWORD"] = connection.Password,
                    ["POSTGRES_MIN_POOL_SIZE"] = "0",
                    [$"Authentication:ExternalProviders:Saml:{TestSamlIdentityProvider.Slug}:Enabled"] = "true",
                    [$"Authentication:ExternalProviders:Saml:{TestSamlIdentityProvider.Slug}:DisplayName"] = "Corp IdP",
                    [$"Authentication:ExternalProviders:Saml:{TestSamlIdentityProvider.Slug}:SpEntityId"] = TestSamlIdentityProvider.SpEntityId,
                    [$"Authentication:ExternalProviders:Saml:{TestSamlIdentityProvider.Slug}:AssertionConsumerServiceUrl"] = TestSamlIdentityProvider.AcsUrl,
                    [$"Authentication:ExternalProviders:Saml:{TestSamlIdentityProvider.Slug}:IdpEntityId"] = TestSamlIdentityProvider.IdpEntityId,
                    [$"Authentication:ExternalProviders:Saml:{TestSamlIdentityProvider.Slug}:IdpSignInServiceUrl"] = TestSamlIdentityProvider.IdpSignInUrl,
                    [$"Authentication:ExternalProviders:Saml:{TestSamlIdentityProvider.Slug}:IdpSingleLogoutServiceUrl"] = TestSamlIdentityProvider.IdpSingleLogoutUrl,
                    [$"Authentication:ExternalProviders:Saml:{TestSamlIdentityProvider.Slug}:IdpSigningCertificates:0"] = signingCertificate
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<DbContext>();
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(connectionString, npgsql =>
                        npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
                services.AddHttpLogging(_ => { });
            });
        }
    }
}
