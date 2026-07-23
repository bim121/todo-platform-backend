namespace TodoPlatform.Api.Configuration;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Public issuer URL (must match JWT <c>iss</c>). Browser / host: <c>http://localhost:8180/realms/...</c>.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Optional OIDC discovery URL when Authority is not reachable from the API container
    /// (e.g. <c>http://keycloak:8080/realms/.../.well-known/openid-configuration</c>).
    /// </summary>
    public string? MetadataAddress { get; set; }

    public string Audience { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = true;
}
