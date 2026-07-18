using GameGuild.Commerce.Payments.Models;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments.Queries.GetWalletAuditLog;

/// <summary>
///     Query to get wallet audit log
/// </summary>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record GetWalletAuditLogQuery(Guid WalletId, int Page, int PageSize) : IQuery<WalletAuditLogResponse>;
