using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InvestAPI.Filters;

public class SecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
            || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ?? false);

        if (hasAllowAnonymous)
        {
            operation.Security = new List<OpenApiSecurityRequirement>();
            operation.Description = PrependDescription(operation.Description, "**Public route** - no authentication required.");
            return;
        }

        var hasAuthorize = context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
            || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ?? false);

        if (hasAuthorize)
        {
            operation.Description = PrependDescription(operation.Description, "**Protected route** - requires Bearer token.");
        }
    }

    private static string PrependDescription(string? existing, string prefix)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return prefix;
        }

        if (existing.StartsWith(prefix, StringComparison.Ordinal))
        {
            return existing;
        }

        return $"{prefix}\n\n{existing}";
    }
}
