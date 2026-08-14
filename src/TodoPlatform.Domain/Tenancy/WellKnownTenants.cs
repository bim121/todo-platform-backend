namespace TodoPlatform.Domain.Tenancy;

/// <summary>Stable seed ids so migrations, seeder, and tests agree.</summary>
public static class WellKnownTenants
{
    public static readonly Guid DefaultId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public const string DefaultSlug = "default";
    public const string DefaultName = "Default";

    public static readonly Guid AcmeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public const string AcmeSlug = "acme-corp";
    public const string AcmeName = "Acme Corp";
}
