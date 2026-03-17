using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InvestAPI.Swagger;

public class AuthorizeDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var api in context.ApiDescriptions)
        {
            if (string.IsNullOrWhiteSpace(api.RelativePath) || string.IsNullOrWhiteSpace(api.HttpMethod))
            {
                continue;
            }

            var path = "/" + api.RelativePath.TrimEnd('/');
            if (!swaggerDoc.Paths.TryGetValue(path, out var pathItem))
            {
                continue;
            }

            if (pathItem.Operations is null)
            {
                continue;
            }

            var operation = pathItem.Operations
                .Where(o => string.Equals(o.Key.ToString(), api.HttpMethod, StringComparison.OrdinalIgnoreCase))
                .Select(o => o.Value)
                .FirstOrDefault();

            if (operation is null)
            {
                continue;
            }

            var metadata = api.ActionDescriptor.EndpointMetadata;
            var hasAllowAnonymous = metadata.OfType<AllowAnonymousAttribute>().Any();
            var hasAuthorize = metadata.OfType<AuthorizeAttribute>().Any();

            if (!hasAllowAnonymous && hasAuthorize)
            {
                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new()
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", swaggerDoc, null)] = new List<string>()
                    }
                };

                operation.Responses ??= new OpenApiResponses();
                operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
            }
            else
            {
                operation.Security = new List<OpenApiSecurityRequirement>();
            }
        }
    }

}
