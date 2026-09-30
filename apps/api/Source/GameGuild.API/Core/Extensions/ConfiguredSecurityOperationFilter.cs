using System.Reflection;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild.API;

/// <summary>
///     Documents explicitly selected ASP.NET authentication schemes on MVC actions.
/// </summary>
internal sealed class ConfiguredSecurityOperationFilter(OpenApiOptions options) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerAttributes = context.MethodInfo.DeclaringType?
            .GetCustomAttributes<AuthorizeAttribute>(true) ?? [];
        var selected = context.MethodInfo.GetCustomAttributes<AuthorizeAttribute>(true)
            .Concat(controllerAttributes)
            .SelectMany(attribute => (attribute.AuthenticationSchemes ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.Ordinal);

        if (selected.Count == 0)
        {
            return;
        }

        var matches = options.SecuritySchemes
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value.AuthenticationScheme) &&
                           selected.Contains(pair.Value.AuthenticationScheme.Trim()))
            .Select(pair => pair.Key)
            .ToList();

        if (options.EnableDefaultBearer && selected.Contains("Bearer"))
        {
            matches.Add("Bearer");
        }

        if (matches.Count == 0)
        {
            return;
        }

        operation.Security = matches.OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = name }
                }] = []
            }).ToList();
    }
}
