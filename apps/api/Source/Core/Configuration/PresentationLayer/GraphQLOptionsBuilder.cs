namespace GameGuild;

public static class GraphQlOptionsBuilder
{
    public static GraphQlOptions Create() { return new GraphQlOptions(); }

    public static GraphQlOptions Create(IConfiguration configuration, string sectionName = "GraphQL")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new GraphQlOptions();

        var section = configuration.GetSection(sectionName);
        if (section.Exists())
        {
            section.Bind(options);
        }

        return options;
    }

    public static GraphQlOptions Build(this GraphQlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        return options;
    }
}
