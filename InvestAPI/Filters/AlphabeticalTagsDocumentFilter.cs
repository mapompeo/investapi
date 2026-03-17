using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InvestAPI.Filters;

public class AlphabeticalTagsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (swaggerDoc.Tags is null || swaggerDoc.Tags.Count == 0)
        {
            return;
        }

        swaggerDoc.Tags = new SortedSet<OpenApiTag>(
            swaggerDoc.Tags,
            Comparer<OpenApiTag>.Create((x, y) =>
                StringComparer.OrdinalIgnoreCase.Compare(x?.Name, y?.Name)));
    }
}
