using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using TodoPlatform.Api.Middleware;

namespace TodoPlatform.Api.Swagger;

/// <summary>
/// Documents <c>X-Tenant-Id</c> on authenticated operations (B-11.6).
/// Optional when JWT includes claim <c>tenant_id</c>; missing on an authenticated call is 400.
/// </summary>
public sealed class TenantIdHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
            return;

        if (!metadata.OfType<IAuthorizeData>().Any())
            return;

        operation.Parameters ??= [];
        if (operation.Parameters.Any(p =>
                string.Equals(p.Name, TenantResolutionMiddleware.HeaderName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = TenantResolutionMiddleware.HeaderName,
            In = ParameterLocation.Header,
            Required = false,
            Description =
                "Tenant UUID or slug (e.g. `default`, `acme-corp`). " +
                "Required for authenticated calls unless the JWT includes claim `tenant_id`. " +
                "Missing → 400; unknown or inactive tenant → 404. " +
                "Do not send tenant in the request body.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }
}
