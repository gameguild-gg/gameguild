using MediatR;
using GameGuild.Modules.Payments.Models;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
/// Query to get user payment methods
/// </summary>
public record GetUserPaymentMethodsQuery : IRequest<IEnumerable<UserFinancialMethod>>
{
    public Guid? UserId { get; init; }
}
