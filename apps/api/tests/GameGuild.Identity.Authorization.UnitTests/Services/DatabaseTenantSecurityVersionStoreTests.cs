using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public sealed class DatabaseTenantSecurityVersionStoreTests
{
    [Fact]
    public async Task IncrementVersionAsync_GlobalScope_UsesReservedEmptyGuid()
    {
        var repository = new Mock<ITenantSecurityVersionRepository>();
        repository.Setup(store => store.IncrementVersionAsync(Guid.Empty, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        var versionStore = new DatabaseTenantSecurityVersionStore(repository.Object);

        var version = await versionStore.IncrementVersionAsync(Guid.Empty.ToString());

        version.Should().Be(4);
        repository.Verify(store => store.IncrementVersionAsync(Guid.Empty, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
