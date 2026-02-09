using GameGuild.Commerce.Payments.Models;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Queries.GetWalletAuditLog;

/// <summary>
///     Query to get wallet audit log
/// </summary>
public sealed record GetWalletAuditLogQuery(Guid WalletId, int Page, int PageSize) : IQuery<WalletAuditLogResponse>;
