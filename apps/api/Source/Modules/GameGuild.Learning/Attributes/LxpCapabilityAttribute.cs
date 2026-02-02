namespace GameGuild.Learning.Attributes;

/// <summary>
/// Attribute to mark endpoints that require a specific LXP capability.
/// Used for feature flag gating of LXP endpoints.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class LxpCapabilityAttribute : Attribute
{
    /// <summary>
    /// The capability key required for this endpoint (e.g., "lxp.discovery").
    /// </summary>
    public string Capability { get; }
    
    /// <summary>
    /// Optional error message when capability is not enabled.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Creates a new LXP capability requirement.
    /// </summary>
    /// <param name="capability">The capability key (e.g., "lxp.discovery", "lxp.learningPaths").</param>
    public LxpCapabilityAttribute(string capability)
    {
        Capability = capability ?? throw new ArgumentNullException(nameof(capability));
    }
}

/// <summary>
/// Known LXP capability keys for type-safe usage.
/// </summary>
public static class LxpCapabilities
{
    /// <summary>LXP Discovery - Featured content, search, browse</summary>
    public const string Discovery = "lxp.discovery";
    
    /// <summary>LXP Learning Paths - Curated course sequences</summary>
    public const string LearningPaths = "lxp.learningPaths";
    
    /// <summary>LXP Basic Recommendations - Simple recommendation engine</summary>
    public const string RecommendationsBasic = "lxp.recommendations.basic";
    
    /// <summary>LXP AI Recommendations - Advanced AI-powered recommendations</summary>
    public const string RecommendationsAI = "lxp.recommendations.ai";
    
    /// <summary>LXP Skills - Skill tracking and management</summary>
    public const string Skills = "lxp.skills";
    
    /// <summary>LXP Social - Social learning features, reviews, comments</summary>
    public const string Social = "lxp.social";
    
    /// <summary>LXP Personalized Feed - User-specific content feed</summary>
    public const string PersonalizedFeed = "lxp.personalizedFeed";
    
    /// <summary>LXP Bookmarks - Save courses for later</summary>
    public const string Bookmarks = "lxp.bookmarks";
    
    /// <summary>LXP Social Proof - Show enrollment counts, ratings</summary>
    public const string SocialProof = "lxp.socialProof";
}
