namespace GameGuild.SharedKernel.Configuration;

public class GraphQlOptions : BaseOptions
{
    public bool EnableGraphQL { get; set; } = false;

    public string Endpoint { get; set; } = "/graphql";

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(Endpoint)) throw new InvalidOperationException("GraphQL endpoint cannot be empty.");
    }

    public static GraphQlOptions CreateDefault() { return new GraphQlOptions(); }
}
