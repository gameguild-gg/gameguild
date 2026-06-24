using GameGuild.Commerce.Subscriptions;

namespace GameGuild.API.Integration;

public sealed class MonthlyStatementAttachmentBuilder(
    IMonthlyStatementDataProvider dataProvider) : IMonthlyStatementAttachmentBuilder
{
    public async Task<MonthlyStatementArtifacts> BuildAsync(
        Guid tenantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var buildContext = await dataProvider
            .BuildAsync(tenantId, fromDate, toDate, cancellationToken)
            .ConfigureAwait(false);

        return MonthlyStatementArtifactComposer.Compose(buildContext);
    }
}
