namespace TodoPlatform.Api.Versioning;

public static class ApiVersionDefaults
{
    public const string HttpContextItemKey = "ApiVersion";
    public const string HeaderName = "Accept-Version";
    public const string DefaultVersion = "v1";

    public static readonly IReadOnlySet<string> SupportedVersions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { DefaultVersion };
}
