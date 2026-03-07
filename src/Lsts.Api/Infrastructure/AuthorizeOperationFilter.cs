using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lsts.Api.Infrastructure;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = (context.ApiDescription.RelativePath ?? "").ToLowerInvariant();
        if (path.StartsWith("api/auth/login") || path.StartsWith("api/public/"))
        {
            operation.Security = new List<OpenApiSecurityRequirement>();
        }
    }
}