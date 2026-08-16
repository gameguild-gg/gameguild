using GameGuild.Learning.Assessments;
using GameGuild.Learning.Courses;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild.API.Setup;

internal sealed class LegacyProgramContentTypeSchemaFilter : ISchemaFilter
{
    private static readonly HashSet<string> LegacyValues =
    [
        nameof(ProgramContentType.Page),
        nameof(ProgramContentType.Challenge),
    ];

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(ProgramContentType) || schema.Enum is null)
            return;

        schema.Enum = schema.Enum
            .Where(value => value is not OpenApiString text || !LegacyValues.Contains(text.Value))
            .ToList();
        schema.Description =
            $"{schema.Description} Legacy values Page and Challenge are normalized on read and are not valid for new content."
                .Trim();
    }
}

internal sealed class LegacyAssessmentTypeSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(AssessmentType) || schema.Enum is null)
            return;

        schema.Enum = schema.Enum
            .Where(value => value is not OpenApiString text || text.Value != nameof(AssessmentType.Exam))
            .ToList();
        schema.Description =
            (schema.Description + " Legacy value Exam is normalized on read and is not valid for new assessments.")
            .Trim();
    }
}
