namespace GameGuild.Identity.Authentication;

internal sealed class OrchestrationChallengeDeniedState : AuthenticationFlowState, ICommitOnFailureOutcome
{
    public bool Success => false;
    public string ErrorCode => "InvalidRiskCode";
    public OrchestrationChallengeDeniedState(AuthenticationFlowState original)
    {
        FlowId = original.FlowId; UserId = original.UserId; CurrentStep = original.CurrentStep;
        RequiredSteps = original.RequiredSteps; CompletedSteps = original.CompletedSteps;
        RiskScore = original.RiskScore; InitiatedAt = original.InitiatedAt; ExpiresAt = original.ExpiresAt;
        IpAddress = original.IpAddress; DeviceFingerprint = original.DeviceFingerprint; StepData = original.StepData;
    }
}
