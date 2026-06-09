namespace TodoPlatform.Api.Versioning;

/// <summary>
/// Marks an endpoint as deprecated. Response will include <c>Deprecation: true</c> and <c>Sunset</c> headers.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class DeprecatedEndpointAttribute(string sunset) : Attribute
{
    /// <summary>
    /// HTTP-date when the endpoint will be removed (RFC 7231).
    /// </summary>
    public string Sunset { get; } = sunset;
}
