using FluentAssertions;

namespace GameGuild.API.UnitTests.Database;

public sealed class EconomySelfServiceRiskDecisionMigrationTests
{
    [Fact]
    public void Migration_InstallsASecurityDefinerIssuerWithFifoEvidenceAndRiskCounterGuards()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "Source", "GameGuild.API", "Database", "Migrations",
            "20260811109000_IssueSelfServiceHardToSoftRiskDecision.Security.cs");
        var sql = File.ReadAllText(path);

        sql.Should().Contain("issue_self_service_hard_to_soft_risk_decision_v1");
        sql.Should().Contain("SECURITY DEFINER");
        sql.Should().Contain("reserve_fifo_fragments_v1");
        sql.Should().Contain("reserve_risk_counter_v1");
        sql.Should().Contain("economy_risk_audit_evidence");
        sql.Should().Contain("financial-crime and trust-safety");
        sql.Should().Contain("REVOKE ALL ON FUNCTION");
        sql.Should().Contain("GRANT EXECUTE");
    }
}
