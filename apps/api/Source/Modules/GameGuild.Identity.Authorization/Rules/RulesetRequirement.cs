
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     A single requirement that represents an entire policy ruleset.
///     The handler will evaluate all rules in the provided or loaded ruleset.
/// </summary>
/// <param name="PolicyName">The name of the policy (e.g., "Users.Edit")</param>
/// <param name="Ruleset">
///     Optional pre-loaded ruleset to avoid double DB load.
///     If null, the handler will load it from the database.
/// </param>
public sealed record RulesetRequirement(
    string PolicyName,
    PolicyRuleset? Ruleset = null) : IAuthorizationRequirement;
