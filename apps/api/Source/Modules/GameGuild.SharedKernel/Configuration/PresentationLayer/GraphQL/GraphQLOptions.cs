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

        if (string.IsNullOrWhiteSpace(Endpoint)) throw new InvalidOperationException("GraphQL endpoint cannot be empty.");
    }

    public static GraphQLOptions CreateDefault() { return new GraphQLOptions(); }
}
