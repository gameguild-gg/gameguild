using FluentAssertions;
using GameGuild.Identity.Users;
using System.Text.Json;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Entities;

public sealed class UserPasswordHistoryTests
{
    [Fact]
    public void SetPasswordHash_RetainsOnlyFiveMostRecentPreviousHashes()
    {
        var user = new User { PasswordHash = "hash-0" };

        for (var index = 1; index <= 7; index++) user.SetPasswordHash($"hash-{index}");

        user.PasswordHash.Should().Be("hash-7");
        user.GetPasswordHistoryHashes().Should().Equal("hash-6", "hash-5", "hash-4", "hash-3", "hash-2");
        user.PasswordHistoryHashes.Should().NotContain("hash-1");
        user.PasswordHistoryHashes.Should().NotContain("hash-0");
    }

    [Fact]
    public void PasswordHashes_AreExcludedFromSerializedUserData()
    {
        var user = new User { PasswordHash = "current-secret-hash" };
        user.SetPasswordHash("new-secret-hash");

        var serialized = JsonSerializer.Serialize(user);

        serialized.Should().NotContain("PasswordHash");
        serialized.Should().NotContain("current-secret-hash");
        serialized.Should().NotContain("new-secret-hash");
    }
}
