using TodoPlatform.Domain.Entities;
using TodoPlatform.Domain.Enums;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Application.Tests.Domain;

public sealed class TenantTests
{
    [Fact]
    public void Create_NormalizesSlug_AndDefaultsActive()
    {
        var tenant = Tenant.Create("Acme-Corp", "Acme", WellKnownTenants.AcmeId);

        Assert.Equal(WellKnownTenants.AcmeId, tenant.Id);
        Assert.Equal("acme-corp", tenant.Slug);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.True(tenant.IsActive);
    }

    [Fact]
    public void Deactivate_MarksInactive()
    {
        var tenant = Tenant.Create("x", "X");
        tenant.Deactivate();
        Assert.False(tenant.IsActive);
    }
}
