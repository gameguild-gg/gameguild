using FluentAssertions;
using GameGuild.Commerce.Payments.Commands.CloseWallet;
using GameGuild.Commerce.Payments.Commands.FreezeWallet;
using GameGuild.Commerce.Payments.Commands.PatchWallet;
using GameGuild.Commerce.Payments.Commands.UnfreezeWallet;
using GameGuild.Commerce.Payments.Queries.GetWalletAuditLog;
using GameGuild.Commerce.Payments.Queries.GetWalletById;
using GameGuild.Commerce.Payments.Queries.ListWallets;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests;

public sealed class WalletAuthorizationContractTests
{
    public static TheoryData<Type> AdministrativeRequests => new()
    {
        typeof(CreateWalletCommand),
        typeof(GetWalletByUserIdQuery),
        typeof(GetWalletBalanceQuery),
        typeof(LockWalletCommand),
        typeof(UnlockWalletCommand),
        typeof(AddFundsCommand),
        typeof(DeductFundsCommand),
        typeof(TransferFundsCommand),
        typeof(CloseWalletCommand),
        typeof(FreezeWalletCommand),
        typeof(UnfreezeWalletCommand),
        typeof(PatchWalletCommand),
        typeof(ListWalletsQuery),
        typeof(GetWalletByIdQuery),
        typeof(GetWalletAuditLogQuery)
    };

    public static TheoryData<Type> SelfServiceRequests => new()
    {
        typeof(CreateMyWalletCommand),
        typeof(GetMyWalletQuery),
        typeof(GetMyWalletBalanceQuery),
        typeof(LockMyWalletCommand),
        typeof(UnlockMyWalletCommand)
    };

    [Theory]
    [MemberData(nameof(AdministrativeRequests))]
    public void AdministrativeRequest_ShouldRequireTheWalletAdminPermission(Type requestType)
    {
        var attribute = requestType.GetCustomAttributes(typeof(AuthorizeRequestAttribute), inherit: true)
            .Cast<AuthorizeRequestAttribute>()
            .Single();

        attribute.Permission.Should().Be(WalletsPermission.Keys.Admin);
    }

    [Theory]
    [MemberData(nameof(SelfServiceRequests))]
    public void SelfServiceRequest_ShouldRequireAnAuthenticatedActor(Type requestType)
    {
        var attribute = requestType.GetCustomAttributes(typeof(AuthorizeRequestAttribute), inherit: true)
            .Cast<AuthorizeRequestAttribute>()
            .Single();

        attribute.Permission.Should().BeEmpty();
    }

    [Fact]
    public void CreateWalletRequest_ShouldNotAcceptAnArbitraryOwner()
    {
        typeof(CreateWalletRequest).GetProperty("UserId").Should().BeNull();
    }
}
