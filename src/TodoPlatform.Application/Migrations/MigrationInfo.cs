namespace TodoPlatform.Application.Migrations;

public sealed record MigrationInfo(
    long Version,
    string Name,
    string Description,
    IReadOnlyList<string> Tags)
{
    public bool IsBeta =>
        Tags.Any(t => string.Equals(t, "beta", StringComparison.OrdinalIgnoreCase));

    /// <summary>OpenAPI <c>schemaVersion</c> label, e.g. V011 or V012-beta-feature.</summary>
    public string SchemaVersionLabel =>
        IsBeta ? $"V{Version:D3}-beta-feature" : $"V{Version:D3}";
}
