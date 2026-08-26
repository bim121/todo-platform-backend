using System.Text.RegularExpressions;

namespace TodoPlatform.Domain.Tenancy;

/// <summary>Maps tenant slug to PostgreSQL schema name (B-12.11).</summary>
public static partial class TenantSchemaNaming
{
    public const string Prefix = "tenant_";

    private static readonly Regex UnsafeSlugChars = UnsafeSlugCharsRegex();
    private static readonly Regex SafeSlugSuffix = SafeSlugSuffixRegex();

    public static string FromSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required.", nameof(slug));

        var normalized = slug.Trim().ToLowerInvariant();
        var sanitized = UnsafeSlugChars.Replace(normalized, "_").Trim('_');

        if (sanitized.Length == 0)
            throw new ArgumentException("Slug must contain at least one alphanumeric character.", nameof(slug));

        return Prefix + sanitized;
    }

    public static bool IsValidSchemaName(string schemaName) =>
        !string.IsNullOrWhiteSpace(schemaName)
        && schemaName.StartsWith(Prefix, StringComparison.Ordinal)
        && SafeSlugSuffix.IsMatch(schemaName[Prefix.Length..]);

    [GeneratedRegex("[^a-z0-9_]", RegexOptions.CultureInvariant)]
    private static partial Regex UnsafeSlugCharsRegex();

    [GeneratedRegex("^[a-z0-9_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeSlugSuffixRegex();
}
