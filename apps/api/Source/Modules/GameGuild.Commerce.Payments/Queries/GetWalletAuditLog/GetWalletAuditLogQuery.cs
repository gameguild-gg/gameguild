using GameGuild.Commerce.Payments.Controllers;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Queries.GetWalletAuditLog;

/// <summary>
///     Query to get wallet audit log
/// </summary>
public record GetWalletAuditLogQuery(Guid WalletId, int Page, int PageSize) : IQuery<WalletAuditLogResponse>;
