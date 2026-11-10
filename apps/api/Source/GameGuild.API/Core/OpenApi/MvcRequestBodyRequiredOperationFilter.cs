using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild.API.Core.OpenApi;

/// <summary>Preserves MVC's body-binding and validation requirements in the public contract.</summary>
public sealed class MvcRequestBodyRequiredOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody is null)
        {
            return;
        }

        // Swashbuckle's controller compatibility path considers explicit attributes only.
        // ApiExplorer also accounts for empty-body binding, while ModelMetadata accounts
        // for validation of a non-nullable parameter even when empty binding is allowed.
        operation.RequestBody.Required |= context.ApiDescription.ParameterDescriptions.Any(parameter =>
            parameter.Source == BindingSource.Body &&
            (parameter.IsRequired || parameter.ModelMetadata?.IsRequired == true));
    }
}
