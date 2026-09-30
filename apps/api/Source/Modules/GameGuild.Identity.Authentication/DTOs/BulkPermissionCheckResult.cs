namespace GameGuild.Identity.Authentication;

/// <summary>
/// The result for one requested permission decision.
/// </summary>
public sealed record BulkPermissionCheckResult(BulkPermissionCheckRequest Request, bool IsGranted);
