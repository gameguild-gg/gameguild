using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Wiring guard G3 — DelegatingHandler lifetimes. IHttpClientFactory requires a
///     FRESH handler instance per pipeline; registering a concrete
///     <see cref="DelegatingHandler"/> as Singleton (or Scoped) makes the second
///     pipeline build throw InvalidOperationException ("The handler must not be
///     reused or cached"). Prevents the lifetime-violation class of gap (inspiring
///     bug: issue #1165 — OpeninaryRetryHandler registered AddSingleton blew up the
///     second outbound pipeline).
///     The check runs over the real host's service descriptors BEFORE any provider
///     is built, so it sees exactly what production registers.
/// </summary>
[Collection(WiringGapHostCollection.Name)]
public sealed class HandlerLifetimeGuardTests(WiringGapHostFixture host)
{
    [Fact]
    public void EveryDelegatingHandler_IsRegisteredTransient()
    {
        var offending = host.Registrations
            .Where(IsDelegatingHandlerRegistration)
            .Where(descriptor => descriptor.Lifetime != ServiceLifetime.Transient)
            .Select(descriptor => WiringGapBaseline.Id(
                WiringGapBaseline.GuardIds.HandlerLifetime,
                $"{descriptor.ServiceType.FullName}|{descriptor.Lifetime}"))
            .ToList();

        WiringGapBaseline.AssertNoNewGaps(
            WiringGapBaseline.GuardIds.HandlerLifetime,
            "every concrete DelegatingHandler must be registered AddTransient: IHttpClientFactory builds a new " +
            "pipeline per client and reusing a singleton/scoped handler instance throws InvalidOperationException " +
            "on the second pipeline. Register services.AddTransient<THandler>() (or rely on AddHttpMessageHandler<T>())",
            offending);
        WiringGapBaseline.AssertNoStaleEntries(
            WiringGapBaseline.GuardIds.HandlerLifetime, offending);
    }

    [Fact]
    public void GuardIsNotVacuous_AtLeastOneDelegatingHandlerIsRegistered()
    {
        // If the product stops registering any DelegatingHandler at all the guard
        // would silently stop checking anything; pin that the scan sees the
        // known handler registrations (issue #1165's blast radius).
        host.Registrations
            .Where(IsDelegatingHandlerRegistration)
            .Should().NotBeEmpty("the host registers outbound pipeline handlers (Openinary, FX rate limiter)");
    }

    private static bool IsDelegatingHandlerRegistration(ServiceDescriptor descriptor) =>
        IsConcreteDelegatingHandler(descriptor.ServiceType)
        || (descriptor.ImplementationType is not null && IsConcreteDelegatingHandler(descriptor.ImplementationType));

    private static bool IsConcreteDelegatingHandler(Type type) =>
        type is { IsClass: true, IsAbstract: false }
        && typeof(DelegatingHandler).IsAssignableFrom(type);
}
