namespace GameGuild.Identity.Authentication;

/// <summary>Server-created denial that retains verified failed-attempt accounting.</summary>
internal sealed class OrchestratedFlowCommittedDenialResponse : OrchestratedFlowStateResponse, ICommitOnFailureOutcome
{
    public OrchestratedFlowCommittedDenialResponse()
    {
        Success = false;
        ErrorCode = "InvalidRiskCode";
    }
}
