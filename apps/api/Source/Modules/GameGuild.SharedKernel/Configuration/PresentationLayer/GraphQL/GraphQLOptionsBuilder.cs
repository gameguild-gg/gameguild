using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration.PresentationLayer.GraphQL;

public static class GraphQLOptionsBuilder
{
    public static GraphQLOptions Create() { return new GraphQLOptions(); }

    public static GraphQLOptions Create(IConfiguration configuration, string sectionName = GraphQLOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new GraphQLOptions();

        var section = configuration.GetSection(sectionName);

        if (section.Exists())
        {
            section.Bind(options);
            // Bind the schema-authorization sub-section explicitly via its declared
            // SectionName so the sub-section key cannot drift from the contract.
            section.GetSection(GraphQLSchemaAuthorizationOptions.SectionName).Bind(options.SchemaAuthorization);
        }

        return options;
    }

    public static GraphQLOptions Build(this GraphQLOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        return options;
    }
}
