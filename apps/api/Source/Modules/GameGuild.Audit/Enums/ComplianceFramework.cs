namespace GameGuild.Modules.Audit.Enums;

/// <summary>
/// Compliance frameworks for audit and evidence packaging
/// </summary>
public enum ComplianceFramework
{
    SOC2Type1 = 0,
    SOC2Type2 = 1,
    ISO27001 = 2,
    GDPR = 3,
    HIPAA = 4,
    PCI_DSS = 5,
    CCPA = 6,
    FedRAMP = 7,
    NIST = 8,
    Custom = 9
}
