namespace GameGuild.Configuration.PresentationLayer.GraphQL;

public sealed class GraphQLOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "GraphQL";

    public bool EnableGraphQL { get; set; } = false;

    public string Endpoint { get; set; } = "/graphql";

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(Endpoint) ||
            !Endpoint.StartsWith("/", StringComparison.Ordinal) ||
            Endpoint.StartsWith("//", StringComparison.Ordinal) ||
            Endpoint.Contains('?') ||
            Endpoint.Contains('#'))
        {
            throw new InvalidOperationException("GraphQL endpoint must be an absolute application path without a query or fragment.");
        }
    }

    public static GraphQLOptions CreateDefault() { return new GraphQLOptions(); }
}
