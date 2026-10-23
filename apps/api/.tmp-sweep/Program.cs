using GameGuild.Compliance.Audit;

var sweep = new EventCoverageSweepService();
var report = sweep.SweepEventCoverage();

Console.WriteLine($"Modules: {report.ModuleCount}");
Console.WriteLine($"Mutating commands: {report.TotalMutatingCommands}");
Console.WriteLine($"Command contracts: {report.TotalCommandContracts}");
Console.WriteLine($"Mutation endpoints: {report.TotalMutationEndpoints}");
Console.WriteLine($"Gaps: {report.Gaps.Count}");
foreach (var gap in report.Gaps)
{
    Console.WriteLine($"  GAP: {gap}");
}

foreach (var module in report.Modules.Where(module => !module.IsFullyCovered))
{
    foreach (var command in module.CommandsMissingHandlers)
    {
        Console.WriteLine($"  NO-HANDLER: {module.AssemblyName} :: {command}");
    }
}

var trail = sweep.BuildAuditTrailCoverage();
Console.WriteLine($"Audit trail families: {trail.Families.Count}");
foreach (var family in trail.Families.Where(family => !family.IsCovered))
{
    Console.WriteLine($"  UNCOVERED FAMILY: {family.Family}");
}

Console.WriteLine($"Event coverage complete: {report.IsComplete}");
Console.WriteLine($"Audit trail coverage complete: {trail.IsComplete}");
return report.IsComplete && trail.IsComplete ? 0 : 1;
