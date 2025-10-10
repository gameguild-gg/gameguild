using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Queries;

public record GetLedgerEntriesByAccountQuery(
    string Account,
    int Skip = 0,
    int Take = 100
) : IRequest<List<FinancialLedgerEntry>>;
