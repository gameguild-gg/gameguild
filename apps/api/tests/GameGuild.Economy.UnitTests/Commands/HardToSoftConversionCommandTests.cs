using FluentAssertions;
using FluentValidation;
using GameGuild.Economy.Commands;
using GameGuild.Economy.Funding;

namespace GameGuild.Economy.UnitTests.Commands;

public sealed class HardToSoftConversionCommandTests
{
    [Fact]
    public async Task Handler_ForwardsTheExactSelfServiceRequestToTheWorkflow()
    {
        var decisionId = Guid.Parse("92000000-0000-0000-0000-000000000001");
        var receipt = new SelfServiceHardToSoftConversionReceipt(
            Guid.Parse("92000000-0000-0000-0000-000000000002"),
            null,
            17,
            "journal-hash",
            false);
        var workflow = new CapturingWorkflow(receipt);
        var handler = new ConvertMyHardToSoftCommandHandler(workflow);
        var command = new ConvertMyHardToSoftCommand(
            new ConvertMyHardToSoftRequest(100, 3, decisionId, "conversion-key"));

        var result = await handler.Handle(command, CancellationToken.None);

        workflow.Request.Should().Be(new SelfServiceHardToSoftConversionRequest(100, 3, decisionId, "conversion-key"));
        result.Should().Be(receipt);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(100, -1, true)]
    [InlineData(100, 0, false)]
    public void Validator_RejectsInvalidCoinAmounts(long principalUnits, long feeUnits, bool shouldFail)
    {
        var command = new ConvertMyHardToSoftCommand(
            new ConvertMyHardToSoftRequest(principalUnits, feeUnits, Guid.NewGuid(), "conversion-key"));

        var result = new ConvertMyHardToSoftCommandValidator().Validate(command);

        result.IsValid.Should().Be(!shouldFail);
    }

    [Fact]
    public void Validator_RequiresRiskDecisionAndIdempotencyKey()
    {
        var command = new ConvertMyHardToSoftCommand(
            new ConvertMyHardToSoftRequest(100, 0, Guid.Empty, string.Empty));

        var result = new ConvertMyHardToSoftCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain(new[]
        {
            "Request.RiskDecisionId",
            "Request.IdempotencyKey"
        });
    }

    private sealed class CapturingWorkflow(SelfServiceHardToSoftConversionReceipt receipt) : IHardToSoftConversionWorkflow
    {
        public SelfServiceHardToSoftConversionRequest? Request { get; private set; }

        public Task<SelfServiceHardToSoftConversionReceipt> ConvertAsync(
            SelfServiceHardToSoftConversionRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(receipt);
        }
    }
}
