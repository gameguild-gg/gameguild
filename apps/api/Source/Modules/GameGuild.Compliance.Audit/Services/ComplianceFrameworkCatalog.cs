namespace GameGuild.Compliance.Audit;

public interface IComplianceFrameworkCatalog
{
    IReadOnlyList<ComplianceFrameworkTemplate> GetTemplates();
    ComplianceFrameworkTemplate? Find(string id);
}

/// <summary>Versioned evidence mappings. Reviewed assessments establish applicability and effectiveness.</summary>
public sealed class ComplianceFrameworkCatalog : IComplianceFrameworkCatalog
{
    private static readonly ComplianceFrameworkTemplate Iso27001 = BuildIso27001();
    public IReadOnlyList<ComplianceFrameworkTemplate> GetTemplates() => [Clone(Iso27001)];
    public ComplianceFrameworkTemplate? Find(string id) => id == Iso27001.Id ? Clone(Iso27001) : null;

    private static ComplianceFrameworkTemplate BuildIso27001()
    {
        const string source = "https://www.iso.org/standard/27001";
        var controls = new List<ComplianceControlTemplate>();
        foreach (var (family, count) in new[] { (5, 37), (6, 8), (7, 14), (8, 34) })
        {
            for (var number = 1; number <= count; number++)
            {
                // These logs are supplemental operational evidence. A reviewed control assessment remains mandatory.
                var evidence = family == 8
                    ? new[] { ComplianceEvidenceKind.Operations, ComplianceEvidenceKind.Authentication, ComplianceEvidenceKind.Authorization, ComplianceEvidenceKind.Integrity }
                    : new[] { ComplianceEvidenceKind.Operations };
                controls.Add(new($"A.{family}.{number}", source, evidence, ["control-assessment"]));
            }
        }
        return new("iso27001-2022-evidence-v1", ComplianceFramework.ISO27001, "ISO/IEC27001:2022",
            ComplianceEvidencePeriodMode.Period,
            [source, "https://committee.iso.org/files/live/sites/jtc1sc27/files/resources/ISO-IECJTC1-SC27_N22394_SC%2027%20Journal%20Volume%202%2C%20Issue%202%20-%20Special%20issue%20on%20ISO-IEC%2027002.pdf"],
            controls,
            [new("control-assessment", ["frameworkVersion", "assessmentStatus", "owner", "controlAssessments"], true, true)]);
    }

    private static ComplianceFrameworkTemplate Clone(ComplianceFrameworkTemplate template) => template with
    {
        Sources = template.Sources.ToArray(),
        Controls = template.Controls.Select(item => item with { AutomaticEvidence = item.AutomaticEvidence.ToArray(), RequiredDocumentTypes = item.RequiredDocumentTypes.ToArray() }).ToArray(),
        Documents = template.Documents.Select(item => item with { RequiredFields = item.RequiredFields.ToArray() }).ToArray()
    };
}
